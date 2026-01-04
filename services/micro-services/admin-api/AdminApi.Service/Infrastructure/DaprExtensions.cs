using AdminApi.Infrastructure.FeatureFlags;
using Dapr.Client;
using Dapr.Extensions.Configuration;

namespace AdminApi.Infrastructure;

public static class DaprExtensions
{
     public static async Task<WebApplicationBuilder> AddDaprServices(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Services.AddDaprClient();

        builder.Configuration.AddDaprSecretStore(
            "vault",
            new DaprClientBuilder().Build(),
            new Dictionary<string, string>()
        );

        // Load configuration from Dapr store
        var daprClient = new DaprClientBuilder().Build();
        var cfg = await daprClient.GetConfiguration("appconfig-" + serviceName, new List<string>());

        // Apply config
        ApplyScopedConfig(builder.Configuration, cfg, "jobboard:config:global:", serviceName);
        ApplyScopedConfig(builder.Configuration, cfg, $"jobboard:config:{serviceName}:", serviceName);

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

    private static void ApplyScopedConfig(
        ConfigurationManager config,
        GetConfigurationResponse cfg,
        string prefix,
        string serviceName)
    {
        foreach (var item in cfg.Items.Where(k => k.Key.StartsWith(prefix)))
        {
            var cleanKey = CleanKey(item.Key, serviceName);
            config[cleanKey] = item.Value.Value;
        }
    }

    private static string CleanKey(string key, string serviceName)
    {
        key = key.Replace($"jobboard:config:{serviceName}:", "");
        key = key.Replace("jobboard:config:global:", "");
        return key;
    }
}