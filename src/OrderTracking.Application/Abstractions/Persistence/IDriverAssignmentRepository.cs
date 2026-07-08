using OrderTracking.Domain.Assignments;

namespace OrderTracking.Application.Abstractions.Persistence;

public interface IDriverAssignmentRepository
{
    Task AddAsync(DriverAssignment assignment, CancellationToken cancellationToken);
    Task<bool> HasActiveForOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task<bool> HasActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken);
    Task<DriverPerformance?> GetPerformanceAsync(Guid driverId, CancellationToken cancellationToken);
}

public sealed record DriverPerformance(Guid DriverId, int CompletedDeliveries, double AverageDeliveryMinutes);
