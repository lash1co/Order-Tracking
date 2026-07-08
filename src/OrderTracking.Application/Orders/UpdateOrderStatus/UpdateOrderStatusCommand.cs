using OrderTracking.Domain.Orders;

namespace OrderTracking.Application.Orders.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(Guid OrderId, OrderStatus Status, string Version);
