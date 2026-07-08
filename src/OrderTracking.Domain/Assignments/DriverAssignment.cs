using OrderTracking.Domain.Common;
using OrderTracking.Domain.Exceptions;

namespace OrderTracking.Domain.Assignments;

public sealed class DriverAssignment : Entity
{
    private DriverAssignment(Guid id) : base(id)
    {
    }

    public Guid OrderId { get; private set; }
    public Guid DriverId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static DriverAssignment Create(Guid orderId, Guid driverId, DateTimeOffset assignedAt)
    {
        if (orderId == Guid.Empty || driverId == Guid.Empty)
            throw new DomainException("Order and driver are required for an assignment.");

        return new DriverAssignment(Guid.NewGuid())
        {
            OrderId = orderId,
            DriverId = driverId,
            AssignedAt = assignedAt.ToUniversalTime()
        };
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (CompletedAt.HasValue)
            throw new DomainException("The assignment is already completed.");
        if (completedAt < AssignedAt)
            throw new DomainException("Completion cannot be before assignment.");

        CompletedAt = completedAt.ToUniversalTime();
    }
}
