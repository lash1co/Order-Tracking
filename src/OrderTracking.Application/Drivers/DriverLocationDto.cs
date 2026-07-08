using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Drivers;

public sealed record DriverLocationDto(
    Guid DriverId,
    string Name,
    string VehicleType,
    string Status,
    double Latitude,
    double Longitude,
    DateTimeOffset UpdatedAt)
{
    public static DriverLocationDto From(Driver driver, DateTimeOffset updatedAt) => new(
        driver.Id,
        driver.Name,
        driver.VehicleType.ToString(),
        driver.Status.ToString(),
        driver.CurrentLocation.Latitude,
        driver.CurrentLocation.Longitude,
        updatedAt.ToUniversalTime());
}
