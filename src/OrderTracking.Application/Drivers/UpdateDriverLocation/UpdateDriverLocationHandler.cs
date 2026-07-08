using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Common.Exceptions;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Drivers.UpdateDriverLocation;

public sealed record UpdateDriverLocationCommand(Guid DriverId, double Latitude, double Longitude);

public sealed class UpdateDriverLocationHandler(IDriverRepository drivers, IUnitOfWork unitOfWork)
{
    public async Task Handle(UpdateDriverLocationCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetByIdAsync(command.DriverId, cancellationToken)
            ?? throw new NotFoundException($"Driver '{command.DriverId}' was not found.");
        driver.UpdateLocation(GeoLocation.Create(command.Latitude, command.Longitude));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
