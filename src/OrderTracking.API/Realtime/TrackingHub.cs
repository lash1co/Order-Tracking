using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrderTracking.API.Realtime;

[Authorize]
public sealed class TrackingHub : Hub
{
    public const string DashboardGroup = "dashboard";

    public async Task SubscribeDashboard(CancellationToken cancellationToken) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup, cancellationToken);

    public async Task UnsubscribeDashboard(CancellationToken cancellationToken) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DashboardGroup, cancellationToken);

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}
