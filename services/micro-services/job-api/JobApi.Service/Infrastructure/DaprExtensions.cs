using Dapr.Client;
using Dapr.Extensions.Configuration;

namespace JobApi.Infrastructure;

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

        // Subscribe for auto-refresh
        var storeName = $"appconfig-{serviceName}";
        await daprClient.SubscribeConfiguration(storeName, new List<string> { "*" });

    
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var config = await daprClient.GetConfiguration(storeName, new List<string>());

                foreach (var kvp in config.Items)
                {
                    if (!kvp.Key.StartsWith($"jobboard:config:{serviceName}") 
                        && !kvp.Key.StartsWith($"jobboard:config:global")) continue;
                    var cleanedKey = CleanKey(kvp.Key, serviceName);
                    builder.Configuration[cleanedKey] = kvp.Value.Value;

                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        });
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