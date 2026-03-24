using System.Diagnostics;
using JobBoard.Application.Interfaces.Configurations;
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

        // Send current feature flags to the connecting client (if service is registered)
        try
        {
            var featureFlagService = serviceProvider.GetService<IFeatureFlagService>();
            if (featureFlagService is not null)
            {
                var flags = await featureFlagService.GetAllFeaturesAsync();
                await Clients.Caller.SendAsync("featureFlagsUpdated", new { flags });
            }
        }
        catch (Exception)
        {
            // Don't kill the SignalR connection if feature flags can't be loaded
        }

        await base.OnConnectedAsync();
    }
}