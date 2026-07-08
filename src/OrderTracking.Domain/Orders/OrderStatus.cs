namespace OrderTracking.Domain.Orders;

public enum OrderStatus
{
    Pending = 1,
    Preparing = 2,
    OutForDelivery = 3,
    Delivered = 4,
    Cancelled = 5
}
