using Dapr.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr;

/// <summary>
/// A background service that watches for changes in configurations stored in a Dapr configuration store
/// </summary>
public sealed class ConfigurationWatcher : BackgroundService
{
    private readonly DaprClient _daprClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationWatcher> _logger;
    private readonly string _serviceName;

    /// <summary>
    /// A background service that monitors configuration changes from a Dapr configuration store
    /// and updates the application's configuration accordingly.
    /// </summary>
    public ConfigurationWatcher(
        DaprClient daprClient,
        IConfiguration configuration,
        ILogger<ConfigurationWatcher> logger,
        string serviceName)
    {
        _daprClient = daprClient;
        _configuration = configuration;
        _logger = logger;
        _serviceName = serviceName;
    }

    /// <summary>
    /// Executes the background task that monitors and updates configurations from a Dapr configuration store.
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
                
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while watching for configuration changes");
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
