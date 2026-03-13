using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace JobBoard.Infrastructure.RedisConfig;

public static class DependencyInjection
{
    private const string DefaultRedisConnection = "192.168.1.160:6379";

    public static async Task<WebApplicationBuilder> AddRedisConfiguration(
        this WebApplicationBuilder builder,
        string serviceName,
        TimeSpan pollInterval)
    {
        var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? DefaultRedisConnection;
        var redis = await ConnectionMultiplexer.ConnectAsync(redisConnection);

        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger("RedisConfig");

        await RedisConfigurationLoader.LoadAsync(builder.Configuration, redis, serviceName, logger);

        builder.Services.AddHostedService(sp =>
            new RedisConfigurationWatcher(
                sp.GetRequiredService<IConnectionMultiplexer>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<RedisConfigurationWatcher>>(),
                serviceName,
                pollInterval,
                sp.GetService<IFeatureFlagNotifier>()));

        return builder;
    }
}
