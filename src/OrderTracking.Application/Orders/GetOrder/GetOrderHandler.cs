using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Common.Exceptions;

namespace OrderTracking.Application.Orders.GetOrder;

public sealed class GetOrderHandler(IOrderRepository orders, IDriverAssignmentRepository assignments)
{
    public async Task<OrderDto> Handle(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Order '{id}' was not found.");

        return OrderDto.From(
            order,
            await assignments.HasActiveForOrderAsync(order.Id, cancellationToken));
    }
}
