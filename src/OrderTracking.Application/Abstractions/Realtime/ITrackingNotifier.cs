using OrderTracking.Application.Drivers;
using OrderTracking.Application.Orders;

namespace OrderTracking.Application.Abstractions.Realtime;

public interface ITrackingNotifier
{
    Task OrderChangedAsync(OrderDto order, CancellationToken cancellationToken);
    Task DriverLocationChangedAsync(DriverLocationDto driverLocation, CancellationToken cancellationToken);
}
