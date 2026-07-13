using OrderTracking.Domain.Common;
using OrderTracking.Domain.Exceptions;

namespace OrderTracking.Domain.Drivers;

public sealed class Driver : Entity
{
    private Driver(Guid id) : base(id)
    {
    }

    public string Name { get; private set; } = string.Empty;
    public VehicleType VehicleType { get; private set; }
    public GeoLocation CurrentLocation { get; private set; } = null!;
    public DriverStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Driver Create(string name, VehicleType vehicleType, GeoLocation location)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Driver name is required.");

        return new Driver(Guid.NewGuid())
        {
            Name = name.Trim(),
            VehicleType = vehicleType,
            CurrentLocation = location,
            Status = DriverStatus.Offline
        };
    }

    public void UpdateLocation(GeoLocation location) => CurrentLocation = location;

    public void ChangeStatus(DriverStatus status)
    {
        if (Status == DriverStatus.Delivering && status == DriverStatus.Available)
            throw new DomainException("A delivering driver must complete the assignment first.");

        Status = status;
    }

    public void CompleteDelivery() => Status = DriverStatus.Available;
}
