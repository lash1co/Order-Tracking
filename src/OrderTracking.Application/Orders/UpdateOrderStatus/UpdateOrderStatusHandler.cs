using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Abstractions.Messaging;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Common.Exceptions;
using OrderTracking.Application.Drivers;
using OrderTracking.Application.Events;
using OrderTracking.Domain.Drivers;
using OrderTracking.Domain.Orders;

namespace OrderTracking.Application.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusHandler(
    IOrderRepository orders,
    IDriverRepository drivers,
    IDriverAssignmentRepository assignments,
    IUnitOfWork unitOfWork,
    IActiveOrdersCache cache,
    ITrackingNotifier notifier,
    IOrderTrackingEventPublisher eventPublisher)
{
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{command.OrderId}' was not found.");

        byte[] expectedVersion;
        try
        {
            expectedVersion = Convert.FromBase64String(command.Version);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Version must be a valid Base64 row version.", nameof(command), exception);
        }

        if (!order.RowVersion.SequenceEqual(expectedVersion))
            throw new ConflictException("The order was modified by another request. Refresh and retry.");

        var activeAssignment = await assignments.GetActiveForOrderAsync(order.Id, cancellationToken);
        var hasDriver = activeAssignment is not null;
        order.ChangeStatus(command.Status, DateTimeOffset.UtcNow, hasDriver);

        Driver? assignedDriver = null;
        if (activeAssignment is not null &&
            command.Status is OrderStatus.OutForDelivery or OrderStatus.Delivered)
        {
            assignedDriver = await drivers.GetByIdAsync(activeAssignment.DriverId, cancellationToken)
                ?? throw new NotFoundException($"Driver '{activeAssignment.DriverId}' was not found.");

            if (command.Status == OrderStatus.OutForDelivery)
                assignedDriver.ChangeStatus(DriverStatus.Delivering);

            if (command.Status == OrderStatus.Delivered)
            {
                activeAssignment.Complete(DateTimeOffset.UtcNow);
                assignedDriver.CompleteDelivery();
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = OrderDto.From(order);
        var occurredAt = DateTimeOffset.UtcNow;
        await cache.InvalidateAsync(cancellationToken);
        await notifier.OrderChangedAsync(dto, cancellationToken);
        await eventPublisher.PublishAsync(
            new OrderStatusChangedEvent(order.Id, order.Status, occurredAt, order.EstimatedDelivery, order.ActualDelivery),
            cancellationToken);

        if (assignedDriver is not null)
        {
            await notifier.DriverLocationChangedAsync(DriverLocationDto.From(assignedDriver, occurredAt), cancellationToken);
            await eventPublisher.PublishAsync(
                new DriverLocationChangedEvent(
                    assignedDriver.Id,
                    assignedDriver.CurrentLocation.Latitude,
                    assignedDriver.CurrentLocation.Longitude,
                    assignedDriver.Status.ToString(),
                    occurredAt),
                cancellationToken);
        }

        return dto;
    }
}
