namespace JobBoard.infrastructure.Dapr;

/// <summary>
/// Represents a notifier for feature flag updates.
/// Used to propagate changes or updates in feature flags across the system.
/// </summary>
public interface IFeatureFlagNotifier
{
    /// <summary>
    /// Notifies subscribers of updates to feature flags.
    /// </summary>
    /// <param name="flags">A read-only collection of feature flags that have been updated.</param>
    /// <returns>A task that represents the asynchronous notify operation.</returns>
    Task NotifyAsync(IReadOnlyDictionary<string,bool> flags);
}