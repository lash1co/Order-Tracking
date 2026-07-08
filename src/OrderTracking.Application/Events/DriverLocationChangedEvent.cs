namespace OrderTracking.Application.Events;

public sealed record DriverLocationChangedEvent(
    Guid DriverId,
    double Latitude,
    double Longitude,
    string Status,
    DateTimeOffset OccurredAt);
