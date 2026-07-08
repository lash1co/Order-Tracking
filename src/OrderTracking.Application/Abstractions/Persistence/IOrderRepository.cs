using OrderTracking.Domain.Orders;

namespace OrderTracking.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetActiveAsync(int skip, int take, CancellationToken cancellationToken);
}
