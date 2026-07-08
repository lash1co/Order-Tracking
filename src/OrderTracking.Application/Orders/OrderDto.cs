using OrderTracking.Domain.Orders;

namespace OrderTracking.Application.Orders;

public sealed record OrderItemDto(Guid Id, Guid MenuItemId, int Quantity, decimal Price);

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    Guid RestaurantId,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset EstimatedDelivery,
    DateTimeOffset? ActualDelivery,
    string Version,
    IReadOnlyCollection<OrderItemDto> Items)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.CustomerId,
        order.RestaurantId,
        order.Status,
        order.CreatedAt,
        order.EstimatedDelivery,
        order.ActualDelivery,
        Convert.ToBase64String(order.RowVersion),
        order.Items.Select(item => new OrderItemDto(item.Id, item.MenuItemId, item.Quantity, item.Price)).ToArray());
}
