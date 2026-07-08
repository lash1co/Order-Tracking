using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Orders;

namespace OrderTracking.Infrastructure.Caching;

internal sealed class NoOpActiveOrdersCache : IActiveOrdersCache
{
    public Task<IReadOnlyList<OrderDto>?> GetAsync(int skip, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OrderDto>?>(null);

    public Task SetAsync(int skip, int take, IReadOnlyList<OrderDto> orders, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task InvalidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
