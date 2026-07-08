using OrderTracking.Domain.Orders;

namespace OrderTracking.Application.Events;

public sealed record OrderStatusChangedEvent(
    Guid OrderId,
    OrderStatus Status,
    DateTimeOffset OccurredAt,
    DateTimeOffset EstimatedDelivery,
    DateTimeOffset? ActualDelivery);
