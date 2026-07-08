using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Common.Exceptions;

namespace OrderTracking.Application.Drivers.GetDriverPerformance;

public sealed class GetDriverPerformanceHandler(IDriverAssignmentRepository assignments)
{
    public async Task<DriverPerformanceDto> Handle(Guid driverId, CancellationToken cancellationToken)
    {
        var result = await assignments.GetPerformanceAsync(driverId, cancellationToken)
            ?? throw new NotFoundException($"Driver '{driverId}' was not found.");
        return new DriverPerformanceDto(result.DriverId, result.CompletedDeliveries, result.AverageDeliveryMinutes);
    }
}
