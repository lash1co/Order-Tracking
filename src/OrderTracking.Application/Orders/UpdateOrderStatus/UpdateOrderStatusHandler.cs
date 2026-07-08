using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Common.Exceptions;

namespace OrderTracking.Application.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusHandler(
    IOrderRepository orders,
    IDriverAssignmentRepository assignments,
    IUnitOfWork unitOfWork)
{
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{command.OrderId}' was not found.");

        byte[] expectedVersion;
        try
        {
            expectedVersion = Convert.FromBase64String(command.Version);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Version must be a valid Base64 row version.", nameof(command), exception);
        }

        if (!order.RowVersion.SequenceEqual(expectedVersion))
            throw new ConflictException("The order was modified by another request. Refresh and retry.");

        var hasDriver = await assignments.HasActiveForOrderAsync(order.Id, cancellationToken);
        order.ChangeStatus(command.Status, DateTimeOffset.UtcNow, hasDriver);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}
