using Dapr.Client;

namespace AdminApi.Infrastructure.FeatureFlags;

/// <summary>
/// A background service that watches for changes in feature flags stored in a Dapr configuration store
/// and propagates updates to the application using a feature flag notifier.
/// </summary>
public sealed class FeatureFlagWatcher : BackgroundService
{
    private readonly DaprClient _daprClient;
    private readonly IConfiguration _configuration;
    private readonly IFeatureFlagNotifier _notifier;
    private readonly ILogger<FeatureFlagWatcher> logger;
    private readonly string _serviceName;

    /// <summary>
    /// A background service that monitors feature flag changes from a Dapr configuration store
    /// and updates the application's configuration and feature flags accordingly.
    /// </summary>
    public FeatureFlagWatcher(
        DaprClient daprClient,
        IConfiguration configuration,
        IFeatureFlagNotifier notifier,
        ILogger<FeatureFlagWatcher> logger,
        string serviceName )
    {
        _daprClient = daprClient;
        _configuration = configuration;
        _notifier = notifier;
        _serviceName = serviceName;
        this.logger = logger;
    }

    /// <summary>
    /// Executes the background task that monitors and updates feature flags from a Dapr configuration store.
    /// </summary>
    /// <param name="stoppingToken">A token that signals when the background operation should stop.</param>
    /// <returns>A task that represents the asynchronous execution of the background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var storeName = $"appconfig-{_serviceName}";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var featureFlags = new Dictionary<string, bool>();
                var config = await _daprClient.GetConfiguration(
                    storeName,
                    new List<string>(),
                    cancellationToken: stoppingToken);

                foreach (var kvp in config.Items)
                {
                    if (!kvp.Key.StartsWith($"jobboard:config:{_serviceName}") &&
                        !kvp.Key.StartsWith("jobboard:config:global"))
                        continue;

                    var cleanedKey = CleanKey(kvp.Key, _serviceName);

                    if (cleanedKey.StartsWith("FeatureFlags:"))
                    {
                        var isEnabled = bool.TryParse(kvp.Value.Value, out var enabled) && enabled;
                        featureFlags[cleanedKey.Replace("FeatureFlags:", "")] = isEnabled;
                    }

                    _configuration[cleanedKey] = kvp.Value.Value;
                }

                await _notifier.NotifyAsync(featureFlags);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while watching for feature flag changes");
            }
        }
    }

    private static string CleanKey(string key, string serviceName)
    {
        key = key.Replace($"jobboard:config:{serviceName}:", "");
        key = key.Replace("jobboard:config:global:", "");
        return key;
    }
}
