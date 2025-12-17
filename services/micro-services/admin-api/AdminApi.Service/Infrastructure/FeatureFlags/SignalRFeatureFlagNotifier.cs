using Microsoft.AspNetCore.SignalR;

namespace AdminApi.Infrastructure.FeatureFlags;

/// <summary>
/// A notifier that uses SignalR to propagate feature flag update notifications to connected clients.
/// Implements the <see cref="IFeatureFlagNotifier"/> interface to provide real-time updates
/// for feature flag changes.
/// </summary>
public class SignalRFeatureFlagNotifier(IHubContext<NotificationsHub> hub): IFeatureFlagNotifier
{
    /// <summary>
    /// Notifies all connected clients about updated feature flags.
    /// Sends a message to all clients with the updated feature flags information.
    /// </summary>
    /// <param name="flags">The collection of feature flag names that have been updated.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    public Task NotifyAsync(IReadOnlyDictionary<string, bool> flags)
    {
        return hub.Clients.All.SendAsync(
            "featureFlagsUpdated",
            new { flags }
        );
    }
}