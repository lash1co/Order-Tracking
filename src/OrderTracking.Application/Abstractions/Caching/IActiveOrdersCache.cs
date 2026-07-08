using OrderTracking.Application.Orders;

namespace OrderTracking.Application.Abstractions.Caching;

public interface IActiveOrdersCache
{
    Task<IReadOnlyList<OrderDto>?> GetAsync(int skip, int take, CancellationToken cancellationToken);
    Task SetAsync(int skip, int take, IReadOnlyList<OrderDto> orders, CancellationToken cancellationToken);
    Task InvalidateAsync(CancellationToken cancellationToken);
}
