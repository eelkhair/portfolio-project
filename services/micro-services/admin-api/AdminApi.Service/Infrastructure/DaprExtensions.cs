using AdminApi.Core;
using AdminApi.Infrastructure.FeatureFlags;
using Dapr.Client;

namespace AdminApi.Infrastructure;

public static class DaprExtensions
{
     public static async Task<WebApplicationBuilder> AddDaprServices(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        // Core Dapr setup (DaprClient, secrets, configuration) — shared with MCP
        await builder.AddDaprCoreServices(serviceName);

        // API-specific: feature flag watcher + SignalR notifier
        builder.Services.AddSingleton<IFeatureFlagNotifier, SignalRFeatureFlagNotifier>();
        builder.Services.AddHostedService(sp =>
            new FeatureFlagWatcher(
                sp.GetRequiredService<DaprClient>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IFeatureFlagNotifier>(),
                sp.GetRequiredService<ILogger<FeatureFlagWatcher>>(),
                serviceName));

        return builder;
    }
}
