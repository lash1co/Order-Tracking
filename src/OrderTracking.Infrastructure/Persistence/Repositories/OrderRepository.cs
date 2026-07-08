using Microsoft.EntityFrameworkCore;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Orders;

namespace OrderTracking.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(OrderTrackingDbContext dbContext) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        dbContext.Orders.AddAsync(order, cancellationToken).AsTask();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders.Include(order => order.Items).SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetActiveAsync(int skip, int take, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Cancelled)
            .OrderByDescending(order => order.CreatedAt)
            .ThenBy(order => order.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
}
