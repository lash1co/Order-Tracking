using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrderTracking.API.Realtime;

[Authorize]
public sealed class TrackingHub : Hub
{
    public const string DashboardGroup = "dashboard";

    public async Task SubscribeDashboard() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup, Context.ConnectionAborted);

    public async Task UnsubscribeDashboard() =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DashboardGroup, Context.ConnectionAborted);

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}
