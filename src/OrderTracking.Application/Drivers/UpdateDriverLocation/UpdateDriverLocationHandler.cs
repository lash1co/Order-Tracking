using OrderTracking.Application.Abstractions.Messaging;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Common.Exceptions;
using OrderTracking.Application.Events;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Drivers.UpdateDriverLocation;

public sealed record UpdateDriverLocationCommand(Guid DriverId, double Latitude, double Longitude);

public sealed class UpdateDriverLocationHandler(
    IDriverRepository drivers,
    IUnitOfWork unitOfWork,
    ITrackingNotifier notifier,
    IOrderTrackingEventPublisher eventPublisher)
{
    public async Task Handle(UpdateDriverLocationCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetByIdAsync(command.DriverId, cancellationToken)
            ?? throw new NotFoundException($"Driver '{command.DriverId}' was not found.");
        driver.UpdateLocation(GeoLocation.Create(command.Latitude, command.Longitude));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var occurredAt = DateTimeOffset.UtcNow;
        await notifier.DriverLocationChangedAsync(DriverLocationDto.From(driver, occurredAt), cancellationToken);
        await eventPublisher.PublishAsync(
            new DriverLocationChangedEvent(
                driver.Id,
                driver.CurrentLocation.Latitude,
                driver.CurrentLocation.Longitude,
                driver.Status.ToString(),
                occurredAt),
            cancellationToken);
    }
}
