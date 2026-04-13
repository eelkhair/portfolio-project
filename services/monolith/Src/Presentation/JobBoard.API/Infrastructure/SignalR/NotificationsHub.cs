using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace JobBoard.API.Infrastructure.SignalR;

/// <summary>
/// Provides real-time notification functionalities for clients via SignalR.
/// This hub manages user connections and group memberships
/// for delivering personalized notifications.
/// </summary>
public class NotificationsHub(ActivitySource activitySource, IServiceProvider serviceProvider) : Hub
{
    /// <summary>
    /// Handles the event triggered when a client establishes a connection to the SignalR hub.
    /// Performs actions such as starting a telemetry activity, identifying the user,
    /// tagging the activity with relevant connection information, and adding the client connection
    /// to a user-specific group based on their identifier.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of handling the connection event.</returns>
    public override async Task OnConnectedAsync()
    {
        using var act = activitySource.StartActivity("signalr.connected");
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.Identity?.Name
                     ?? Context.UserIdentifier;
        act?.SetTag("signalr.connection_id", Context.ConnectionId);
        act?.SetTag("enduser.id", userId);
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        // Send current feature flags to the connecting client.
        // Read directly from IConfiguration (pre-loaded from Redis at startup by RedisConfigurationLoader)
        // so that Redis-backed flags like "Monolith" are included alongside IFeatureManager flags.
        try
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var flags = configuration.GetSection("FeatureFlags")
                .GetChildren()
                .ToDictionary(c => c.Key, c => bool.TryParse(c.Value, out var b) && b, StringComparer.Ordinal);

            if (flags.Count > 0)
                await Clients.Caller.SendAsync("featureFlagsUpdated", new { flags });
        }
        catch (Exception)
        {
            // Don't kill the SignalR connection if feature flags can't be loaded
        }

        await base.OnConnectedAsync();
    }
}
