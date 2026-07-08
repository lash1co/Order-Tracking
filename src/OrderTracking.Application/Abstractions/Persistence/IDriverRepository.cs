using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Abstractions.Persistence;

public interface IDriverRepository
{
    Task AddAsync(Driver driver, CancellationToken cancellationToken);
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DriverDistance>> GetNearestAvailableAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        int take,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Driver>> GetForLocationSimulationAsync(int take, CancellationToken cancellationToken);
}

public sealed record DriverDistance(Driver Driver, double DistanceMeters);
