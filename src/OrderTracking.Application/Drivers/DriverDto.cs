using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Drivers;

public sealed record NearbyDriverDto(
    Guid Id,
    string Name,
    VehicleType VehicleType,
    DriverStatus Status,
    double Latitude,
    double Longitude,
    double DistanceMeters);

public sealed record DriverPerformanceDto(Guid DriverId, int CompletedDeliveries, double AverageDeliveryMinutes);
