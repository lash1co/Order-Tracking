using Microsoft.EntityFrameworkCore;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Infrastructure.Persistence.Repositories;

internal sealed class DriverRepository(OrderTrackingDbContext dbContext) : IDriverRepository
{
    public Task AddAsync(Driver driver, CancellationToken cancellationToken) =>
        dbContext.Drivers.AddAsync(driver, cancellationToken).AsTask();

    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Drivers.SingleOrDefaultAsync(driver => driver.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Driver>> GetActiveAsync(int take, CancellationToken cancellationToken) =>
        await dbContext.Drivers
            .AsNoTracking()
            .Where(driver => driver.Status != DriverStatus.Offline)
            .OrderBy(driver => driver.Name)
            .ThenBy(driver => driver.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DriverDistance>> GetNearestAvailableAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        int take,
        CancellationToken cancellationToken)
    {
        var availableDrivers = await dbContext.Drivers
            .AsNoTracking()
            .Where(driver => driver.Status == DriverStatus.Available)
            .ToListAsync(cancellationToken);

        return availableDrivers
            .Select(driver => new DriverDistance(
                driver,
                CalculateDistanceMeters(latitude, longitude, driver.CurrentLocation.Latitude, driver.CurrentLocation.Longitude)))
            .Where(item => item.DistanceMeters <= radiusMeters)
            .OrderBy(item => item.DistanceMeters)
            .Take(take)
            .ToArray();
    }

    public async Task<IReadOnlyList<Driver>> GetForLocationSimulationAsync(int take, CancellationToken cancellationToken) =>
        await dbContext.Drivers
            .Where(driver => driver.Status != DriverStatus.Offline)
            .OrderBy(driver => driver.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    private static double CalculateDistanceMeters(double originLatitude, double originLongitude, double targetLatitude, double targetLongitude)
    {
        const double earthRadiusMeters = 6_371_000;
        var originLatRadians = ToRadians(originLatitude);
        var targetLatRadians = ToRadians(targetLatitude);
        var deltaLat = ToRadians(targetLatitude - originLatitude);
        var deltaLon = ToRadians(targetLongitude - originLongitude);

        var haversine = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                        Math.Cos(originLatRadians) * Math.Cos(targetLatRadians) *
                        Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var angularDistance = 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
        return earthRadiusMeters * angularDistance;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
