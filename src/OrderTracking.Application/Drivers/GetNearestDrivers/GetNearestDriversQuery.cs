namespace OrderTracking.Application.Drivers.GetNearestDrivers;

public sealed record GetNearestDriversQuery(double Latitude, double Longitude, double RadiusMeters = 5000, int Take = 20);
