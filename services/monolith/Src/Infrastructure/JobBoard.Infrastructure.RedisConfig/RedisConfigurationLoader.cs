using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace JobBoard.Infrastructure.RedisConfig;

public static class RedisConfigurationLoader
{
    public static async Task LoadAsync(
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        string serviceName,
        ILogger logger)
    {
        var db = redis.GetDatabase(1);
        var server = redis.GetServers().First();

        var prefixes = new[]
        {
            "jobboard:config:global:",
            $"jobboard:config:{serviceName}:"
        };

        foreach (var prefix in prefixes)
        {
            var count = 0;
            await foreach (var key in server.KeysAsync(database: 1, pattern: $"{prefix}*"))
            {
                var value = await db.StringGetAsync(key);
                if (value.HasValue)
                {
                    var cleanedKey = CleanKey(key!, serviceName);
                    configuration[cleanedKey] = value!;
                    count++;
                }
            }

            logger.LogInformation("Loaded {Count} config keys from Redis prefix '{Prefix}'", count, prefix);
        }
    }

    internal static string CleanKey(string key, string serviceName)
    {
        key = key.Replace($"jobboard:config:{serviceName}:", "");
        key = key.Replace("jobboard:config:global:", "");
        return key;
    }
}
