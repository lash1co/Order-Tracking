using OrderTracking.Domain.Drivers;
using OrderTracking.Domain.Exceptions;

namespace OrderTracking.UnitTests.Drivers;

public sealed class DriverTests
{
    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Location_OutsideEarthBounds_Throws(double latitude, double longitude)
    {
        Assert.Throws<DomainException>(() => GeoLocation.Create(latitude, longitude));
    }

    [Fact]
    public void Create_WithValidData_StartsOffline()
    {
        var driver = Driver.Create("Ada", VehicleType.Bicycle, GeoLocation.Create(4.711, -74.0721));

        Assert.Equal(DriverStatus.Offline, driver.Status);
        Assert.Equal("Ada", driver.Name);
    }

    [Fact]
    public void CreateWithoutNameThrows()
    {
        Assert.Throws<DomainException>(() =>
            Driver.Create(" ", VehicleType.Car, GeoLocation.Create(0, 0)));
    }

    [Fact]
    public void UpdateLocationStoresNewPosition()
    {
        var driver = Driver.Create("Ada", VehicleType.Bicycle, GeoLocation.Create(0, 0));
        var newLocation = GeoLocation.Create(4.711, -74.0721);

        driver.UpdateLocation(newLocation);

        Assert.Equal(newLocation, driver.CurrentLocation);
    }

    [Fact]
    public void DeliveringDriverCannotBecomeAvailableDirectly()
    {
        var driver = Driver.Create("Ada", VehicleType.Bicycle, GeoLocation.Create(0, 0));
        driver.ChangeStatus(DriverStatus.Delivering);

        Assert.Throws<DomainException>(() => driver.ChangeStatus(DriverStatus.Available));
    }
}
