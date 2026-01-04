using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace AdminApi.Infrastructure;

public class NotificationsHub(ActivitySource activitySource): Hub
{
    public override async Task OnConnectedAsync()
    {
        using var act = activitySource.StartActivity("signalr.connected");
        var userId = Context.User?.FindFirst("sub")?.Value      // Auth0 sub
                     ?? Context.User?.Identity?.Name
                     ?? Context.UserIdentifier;
        act?.SetTag("signalr.connection_id", Context.ConnectionId);
        act?.SetTag("enduser.id", userId);
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }
}