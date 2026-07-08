using Microsoft.AspNetCore.SignalR;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Drivers;
using OrderTracking.Application.Orders;

namespace OrderTracking.API.Realtime;

public sealed class SignalRTrackingNotifier(IHubContext<TrackingHub> hubContext) : ITrackingNotifier
{
    public Task OrderChangedAsync(OrderDto order, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(TrackingHub.DashboardGroup)
            .SendAsync("order.changed", order, cancellationToken);

    public Task DriverLocationChangedAsync(DriverLocationDto driverLocation, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(TrackingHub.DashboardGroup)
            .SendAsync("driver.location.changed", driverLocation, cancellationToken);
}
