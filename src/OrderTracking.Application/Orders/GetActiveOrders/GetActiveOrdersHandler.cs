using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Abstractions.Persistence;

namespace OrderTracking.Application.Orders.GetActiveOrders;

public sealed class GetActiveOrdersHandler(IOrderRepository orders, IActiveOrdersCache cache)
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetActiveOrdersQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(query), "Page must be positive and page size must be between 1 and 200.");

        var skip = (query.Page - 1) * query.PageSize;
        var cached = await cache.GetAsync(skip, query.PageSize, cancellationToken);
        if (cached is not null)
            return cached;

        var result = await orders.GetActiveAsync(skip, query.PageSize, cancellationToken);
        var activeOrders = result.Select(OrderDto.From).ToArray();
        await cache.SetAsync(skip, query.PageSize, activeOrders, cancellationToken);
        return activeOrders;
    }
}
