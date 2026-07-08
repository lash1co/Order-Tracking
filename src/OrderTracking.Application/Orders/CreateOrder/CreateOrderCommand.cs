namespace OrderTracking.Application.Orders.CreateOrder;

public sealed record CreateOrderItem(Guid MenuItemId, int Quantity, decimal Price);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    Guid RestaurantId,
    DateTimeOffset EstimatedDelivery,
    IReadOnlyCollection<CreateOrderItem> Items);
