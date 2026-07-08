using OrderTracking.Domain.Common;
using OrderTracking.Domain.Exceptions;

namespace OrderTracking.Domain.Orders;

public sealed class OrderItem : Entity
{
    private OrderItem(Guid id) : base(id)
    {
    }

    public Guid OrderId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }

    internal static OrderItem Create(Guid orderId, Guid menuItemId, int quantity, decimal price)
    {
        if (menuItemId == Guid.Empty)
            throw new DomainException("Menu item id is required.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (price < 0)
            throw new DomainException("Price cannot be negative.");

        return new OrderItem(Guid.NewGuid())
        {
            OrderId = orderId,
            MenuItemId = menuItemId,
            Quantity = quantity,
            Price = price
        };
    }
}
