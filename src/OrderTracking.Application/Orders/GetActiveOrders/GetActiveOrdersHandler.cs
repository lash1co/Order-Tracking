using OrderTracking.Application.Abstractions.Persistence;

namespace OrderTracking.Application.Orders.GetActiveOrders;

public sealed class GetActiveOrdersHandler(IOrderRepository orders)
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetActiveOrdersQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(query), "Page must be positive and page size must be between 1 and 200.");

        var result = await orders.GetActiveAsync((query.Page - 1) * query.PageSize, query.PageSize, cancellationToken);
        return result.Select(OrderDto.From).ToArray();
    }
}
