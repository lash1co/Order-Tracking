using OrderTracking.Domain.Common;
using OrderTracking.Domain.Exceptions;

namespace OrderTracking.Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];

    private Order(Guid id) : base(id)
    {
    }

    public Guid CustomerId { get; private set; }
    public Guid RestaurantId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset EstimatedDelivery { get; private set; }
    public DateTimeOffset? ActualDelivery { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(
        Guid customerId,
        Guid restaurantId,
        DateTimeOffset createdAt,
        DateTimeOffset estimatedDelivery)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer id is required.");
        if (restaurantId == Guid.Empty)
            throw new DomainException("Restaurant id is required.");
        if (estimatedDelivery <= createdAt)
            throw new DomainException("Estimated delivery must be after creation time.");

        return new Order(Guid.NewGuid())
        {
            CustomerId = customerId,
            RestaurantId = restaurantId,
            Status = OrderStatus.Pending,
            CreatedAt = createdAt.ToUniversalTime(),
            EstimatedDelivery = estimatedDelivery.ToUniversalTime()
        };
    }

    public void AddItem(Guid menuItemId, int quantity, decimal unitPrice)
    {
        EnsureEditable();
        _items.Add(OrderItem.Create(Id, menuItemId, quantity, unitPrice));
    }

    public void ChangeStatus(OrderStatus newStatus, DateTimeOffset occurredAt, bool hasActiveDriverAssignment = false)
    {
        if (newStatus == Status)
            return;

        var isValid = (Status, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Preparing) => true,
            (OrderStatus.Preparing, OrderStatus.OutForDelivery) => hasActiveDriverAssignment,
            (OrderStatus.OutForDelivery, OrderStatus.Delivered) => true,
            (OrderStatus.Pending or OrderStatus.Preparing, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (!isValid)
            throw new DomainException($"Cannot transition order from {Status} to {newStatus}.");

        Status = newStatus;
        if (newStatus == OrderStatus.Delivered)
            ActualDelivery = occurredAt.ToUniversalTime();
    }

    private void EnsureEditable()
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new DomainException("A completed order cannot be modified.");
    }
}
