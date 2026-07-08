using Microsoft.EntityFrameworkCore;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Assignments;

namespace OrderTracking.Infrastructure.Persistence.Repositories;

internal sealed class DriverAssignmentRepository(OrderTrackingDbContext dbContext) : IDriverAssignmentRepository
{
    public Task AddAsync(DriverAssignment assignment, CancellationToken cancellationToken) =>
        dbContext.DriverAssignments.AddAsync(assignment, cancellationToken).AsTask();

    public Task<bool> HasActiveForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.DriverAssignments.AnyAsync(
            assignment => assignment.OrderId == orderId && assignment.CompletedAt == null,
            cancellationToken);

    public Task<bool> HasActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken) =>
        dbContext.DriverAssignments.AnyAsync(
            assignment => assignment.DriverId == driverId && assignment.CompletedAt == null,
            cancellationToken);

    public async Task<DriverPerformance?> GetPerformanceAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var driverExists = await dbContext.Drivers.AnyAsync(driver => driver.Id == driverId, cancellationToken);
        if (!driverExists)
            return null;

        var completed = dbContext.DriverAssignments
            .AsNoTracking()
            .Where(assignment => assignment.DriverId == driverId && assignment.CompletedAt != null);

        var count = await completed.CountAsync(cancellationToken);
        var average = count == 0
            ? 0
            : await completed.AverageAsync(
                assignment => EF.Functions.DateDiffSecond(assignment.AssignedAt, assignment.CompletedAt!.Value) / 60.0,
                cancellationToken);

        return new DriverPerformance(driverId, count, average);
    }
}
