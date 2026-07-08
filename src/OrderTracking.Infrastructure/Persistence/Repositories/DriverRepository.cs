using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Infrastructure.Persistence.Repositories;

internal sealed class DriverRepository(OrderTrackingDbContext dbContext) : IDriverRepository
{
    public Task AddAsync(Driver driver, CancellationToken cancellationToken) =>
        dbContext.Drivers.AddAsync(driver, cancellationToken).AsTask();

    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Drivers.SingleOrDefaultAsync(driver => driver.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DriverDistance>> GetNearestAvailableAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        int take,
        CancellationToken cancellationToken)
    {
        var searchPoint = new Point(longitude, latitude) { SRID = 4326 };
        return await dbContext.Drivers
            .AsNoTracking()
            .Where(driver => driver.Status == DriverStatus.Available)
            .Select(driver => new DriverDistance(
                driver,
                EF.Property<Point>(driver, "Location").Distance(searchPoint)))
            .Where(item => item.DistanceMeters <= radiusMeters)
            .OrderBy(item => item.DistanceMeters)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
