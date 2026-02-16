using JobBoard.Infrastructure.Diagnostics.Observability;
using Microsoft.AspNetCore.SignalR;

#pragma warning disable CS1591

namespace JobBoard.AI.API.Infrastructure.SignalR;

public class AiNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        using var act = TracingFilters.Source.StartActivity("signalr.connected");

        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.Identity?.Name
                     ?? Context.UserIdentifier;

        act?.SetTag("signalr.connection_id", Context.ConnectionId);
        act?.SetTag("enduser.id", userId);

        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }
}
