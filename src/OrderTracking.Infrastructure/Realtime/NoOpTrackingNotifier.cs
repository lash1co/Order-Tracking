using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Drivers;
using OrderTracking.Application.Orders;

namespace OrderTracking.Infrastructure.Realtime;

internal sealed class NoOpTrackingNotifier : ITrackingNotifier
{
    public Task OrderChangedAsync(OrderDto order, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DriverLocationChangedAsync(DriverLocationDto driverLocation, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
