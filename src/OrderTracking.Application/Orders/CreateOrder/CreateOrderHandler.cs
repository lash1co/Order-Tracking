using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Orders;

namespace OrderTracking.Application.Orders.CreateOrder;

public sealed class CreateOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
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
        return OrderDto.From(order);
    }
}
