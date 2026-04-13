using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace JobBoard.Infrastructure.RedisConfig;

public sealed class RedisConfigurationWatcher : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;
    private readonly IFeatureFlagNotifier? _notifier;
    private readonly ILogger<RedisConfigurationWatcher> _logger;
    private readonly string _serviceName;
    private readonly TimeSpan _pollInterval;
    private readonly int _databaseId;

    public RedisConfigurationWatcher(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisConfigurationWatcher> logger,
        string serviceName,
        TimeSpan pollInterval,
        int databaseId = 1,
        IFeatureFlagNotifier? notifier = null)
    {
        _redis = redis;
        _configuration = configuration;
        _notifier = notifier;
        _logger = logger;
        _serviceName = serviceName;
        _pollInterval = pollInterval;
        _databaseId = databaseId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var db = _redis.GetDatabase(_databaseId);
                var server = _redis.GetServers().First();

                var featureFlags = new Dictionary<string, bool>(StringComparer.Ordinal);
                var prefixes = new[]
                {
                    "jobboard:config:global:",
                    $"jobboard:config:{_serviceName}:"
                };

                foreach (var prefix in prefixes)
                {
                    await foreach (var key in server.KeysAsync(database: _databaseId, pattern: $"{prefix}*").WithCancellation(stoppingToken))
                    {
                        var value = await db.StringGetAsync(key);
                        if (!value.HasValue) continue;

                        var cleanedKey = RedisConfigurationLoader.CleanKey(key!, _serviceName);
                        _configuration[cleanedKey] = value!;

                        if (cleanedKey.StartsWith("FeatureFlags:", StringComparison.Ordinal))
                        {
                            var isEnabled = bool.TryParse(value!, out var enabled) && enabled;
                            featureFlags[cleanedKey.Replace("FeatureFlags:", "")] = isEnabled;
                        }
                    }
                }

                if (_notifier is not null && featureFlags.Count > 0)
                {
                    await _notifier.NotifyAsync(featureFlags);
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while watching Redis configuration");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
