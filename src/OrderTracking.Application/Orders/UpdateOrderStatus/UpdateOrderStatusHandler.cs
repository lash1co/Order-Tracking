using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Abstractions.Messaging;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Common.Exceptions;
using OrderTracking.Application.Events;

namespace OrderTracking.Application.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusHandler(
    IOrderRepository orders,
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

        var hasDriver = await assignments.HasActiveForOrderAsync(order.Id, cancellationToken);
        order.ChangeStatus(command.Status, DateTimeOffset.UtcNow, hasDriver);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = OrderDto.From(order);
        await cache.InvalidateAsync(cancellationToken);
        await notifier.OrderChangedAsync(dto, cancellationToken);
        await eventPublisher.PublishAsync(
            new OrderStatusChangedEvent(order.Id, order.Status, DateTimeOffset.UtcNow, order.EstimatedDelivery, order.ActualDelivery),
            cancellationToken);
        return dto;
    }
}
