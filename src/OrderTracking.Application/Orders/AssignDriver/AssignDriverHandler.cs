using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Common.Exceptions;
using OrderTracking.Domain.Assignments;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Orders.AssignDriver;

public sealed record AssignDriverCommand(Guid OrderId, Guid DriverId);

public sealed class AssignDriverHandler(
    IOrderRepository orders,
    IDriverRepository drivers,
    IDriverAssignmentRepository assignments,
    IUnitOfWork unitOfWork)
{
    public async Task<Guid> Handle(AssignDriverCommand command, CancellationToken cancellationToken)
    {
        _ = await orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{command.OrderId}' was not found.");
        var driver = await drivers.GetByIdAsync(command.DriverId, cancellationToken)
            ?? throw new NotFoundException($"Driver '{command.DriverId}' was not found.");

        if (driver.Status != DriverStatus.Available ||
            await assignments.HasActiveForOrderAsync(command.OrderId, cancellationToken) ||
            await assignments.HasActiveForDriverAsync(command.DriverId, cancellationToken))
            throw new ConflictException("The order or driver already has an active assignment.");

        var assignment = DriverAssignment.Create(command.OrderId, command.DriverId, DateTimeOffset.UtcNow);
        driver.ChangeStatus(DriverStatus.Assigned);
        await assignments.AddAsync(assignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return assignment.Id;
    }
}
