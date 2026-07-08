using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Application.Drivers.GetNearestDrivers;

public sealed class GetNearestDriversHandler(IDriverRepository drivers)
{
    public async Task<IReadOnlyList<NearbyDriverDto>> Handle(GetNearestDriversQuery query, CancellationToken cancellationToken)
    {
        _ = GeoLocation.Create(query.Latitude, query.Longitude);
        if (query.RadiusMeters is <= 0 or > 50_000 || query.Take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(query), "Radius or result limit is outside the allowed range.");

        var result = await drivers.GetNearestAvailableAsync(
            query.Latitude, query.Longitude, query.RadiusMeters, query.Take, cancellationToken);

        return result.Select(item => new NearbyDriverDto(
            item.Driver.Id,
            item.Driver.Name,
            item.Driver.VehicleType,
            item.Driver.Status,
            item.Driver.CurrentLocation.Latitude,
            item.Driver.CurrentLocation.Longitude,
            item.DistanceMeters)).ToArray();
    }
}
