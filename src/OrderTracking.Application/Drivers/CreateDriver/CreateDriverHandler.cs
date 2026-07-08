using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Drivers.CreateDriver;

public sealed record CreateDriverCommand(string Name, VehicleType VehicleType, double Latitude, double Longitude);

public sealed class CreateDriverHandler(IDriverRepository drivers, IUnitOfWork unitOfWork)
{
    public async Task<Guid> Handle(CreateDriverCommand command, CancellationToken cancellationToken)
    {
        var driver = Driver.Create(
            command.Name,
            command.VehicleType,
            GeoLocation.Create(command.Latitude, command.Longitude));
        driver.ChangeStatus(DriverStatus.Available);
        await drivers.AddAsync(driver, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return driver.Id;
    }
}
