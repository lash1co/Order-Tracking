using OrderTracking.Domain.Assignments;
using OrderTracking.Domain.Exceptions;

namespace OrderTracking.UnitTests.Assignments;

public sealed class DriverAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateWithValidDataStoresAssignment()
    {
        var orderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var assignment = DriverAssignment.Create(orderId, driverId, Now);

        Assert.Equal(orderId, assignment.OrderId);
        Assert.Equal(driverId, assignment.DriverId);
        Assert.Null(assignment.CompletedAt);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CreateWithMissingReferenceThrows(bool missingOrder, bool missingDriver)
    {
        var orderId = missingOrder ? Guid.Empty : Guid.NewGuid();
        var driverId = missingDriver ? Guid.Empty : Guid.NewGuid();

        Assert.Throws<DomainException>(() => DriverAssignment.Create(orderId, driverId, Now));
    }

    [Fact]
    public void CompleteStoresCompletionTime()
    {
        var assignment = CreateAssignment();

        assignment.Complete(Now.AddMinutes(20));

        Assert.Equal(Now.AddMinutes(20), assignment.CompletedAt);
    }

    [Fact]
    public void CompleteTwiceThrows()
    {
        var assignment = CreateAssignment();
        assignment.Complete(Now.AddMinutes(20));

        Assert.Throws<DomainException>(() => assignment.Complete(Now.AddMinutes(30)));
    }

    [Fact]
    public void CompleteBeforeAssignmentThrows()
    {
        var assignment = CreateAssignment();

        Assert.Throws<DomainException>(() => assignment.Complete(Now.AddMinutes(-1)));
    }

    private static DriverAssignment CreateAssignment() =>
        DriverAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
}
