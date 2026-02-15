using Dapr.Client;
using Dapr.Extensions.Configuration;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.Dapr.AITools;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admin;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr;

public static class DependencyInjection
{
    public static async Task<WebApplicationBuilder> AddDaprServices(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        // Dapr client
        builder.Services.AddDaprClient();
        builder.Services.AddKeyedScoped<IAiTools, MonolithToolRegistry>("monolith");
        builder.Services.AddKeyedScoped<IAiTools, AdminToolRegistry>("micro");
        builder.Services.AddScoped<IMonolithApiClient, MonolithApiClient>();
        builder.Services.AddScoped<IAdminApiClient, AdminApiClient>();
        // Vault secrets
        builder.Configuration.AddDaprSecretStore(
            "vault",
            new DaprClientBuilder().Build(),
            new Dictionary<string, string>()
        );

        // Initial config load (startup)
        var daprClient = new DaprClientBuilder().Build();
        var storeName = $"appconfig-{serviceName}";

        var cfg = await daprClient.GetConfiguration(storeName, new List<string>());

        ApplyScopedConfig(builder.Configuration, cfg, "jobboard:config:global:", serviceName);
        ApplyScopedConfig(builder.Configuration, cfg, $"jobboard:config:{serviceName}:", serviceName);

        // Background watcher (CORRECT)
        builder.Services.AddHostedService(sp =>
            new ConfigurationWatcher(
                sp.GetRequiredService<DaprClient>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<ConfigurationWatcher>>(),
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
