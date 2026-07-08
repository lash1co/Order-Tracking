using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Abstractions.Messaging;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Events;
using OrderTracking.Domain.Orders;

namespace OrderTracking.Application.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IActiveOrdersCache cache,
    ITrackingNotifier notifier,
    IOrderTrackingEventPublisher eventPublisher)
{
    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Items.Count == 0)
            throw new ArgumentException("An order requires at least one item.", nameof(command));

        var now = DateTimeOffset.UtcNow;
        var order = Order.Create(command.CustomerId, command.RestaurantId, now, command.EstimatedDelivery);
        foreach (var item in command.Items)
            order.AddItem(item.MenuItemId, item.Quantity, item.Price);

        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = OrderDto.From(order);
        await cache.InvalidateAsync(cancellationToken);
        await notifier.OrderChangedAsync(dto, cancellationToken);
        await eventPublisher.PublishAsync(
            new OrderStatusChangedEvent(order.Id, order.Status, now, order.EstimatedDelivery, order.ActualDelivery),
            cancellationToken);
        return dto;
    }
}
