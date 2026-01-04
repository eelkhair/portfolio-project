namespace JobBoard.API.Infrastructure.SignalR.FeatureFlags;

/// <summary>
/// Represents a notification or event that is raised when feature flags are updated.
/// </summary>
/// <remarks>
/// This class is used to convey information about which feature flags are currently enabled.
/// It is typically used in signalr-based communication to notify clients of changes in feature flags.
/// </remarks>
/// <param name="EnabledFlags">A collection of feature flag names that are currently enabled.</param>
public record FeatureFlagsUpdated(
    IReadOnlyCollection<string> EnabledFlags
);