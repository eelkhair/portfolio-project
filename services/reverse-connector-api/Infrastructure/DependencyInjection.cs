using System.Text.Json;
using Dapr.Client;
using Dapr.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using ReverseConnectorAPI.Clients;

namespace ReverseConnectorAPI;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Reverse Connector API",
                Version = "v1",
                Description = "Reverse sync: microservices → monolith"
            });
        });

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddHttpClient<MonolithHttpClient>((sp, client) =>
        {
            var monolithUrl = sp.GetRequiredService<IConfiguration>()["MonolithUrl"] ?? "http://monolith-api:8080";
            client.BaseAddress = new Uri(monolithUrl);

            var apiKey = sp.GetRequiredService<IConfiguration>()["InternalApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        });

        return services;
    }

    public static async Task<WebApplicationBuilder> AddDaprServices(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Services.AddDaprClient();

        try
        {
            builder.Configuration.AddDaprSecretStore(
                "vault",
                new DaprClientBuilder().Build(),
                new Dictionary<string, string>(StringComparer.Ordinal)
            );

            var daprClient = new DaprClientBuilder().Build();
            var cfg = await daprClient.GetConfiguration("appconfig-" + serviceName, new List<string>());

            ApplyScopedConfig(builder.Configuration, cfg, "jobboard:config:global:", serviceName);
            ApplyScopedConfig(builder.Configuration, cfg, $"jobboard:config:{serviceName}:", serviceName);
        }
        catch (Exception)
        {
            // Dapr config store not available — fall back to appsettings.json
        }

        return builder;
    }

    private static void ApplyScopedConfig(
        ConfigurationManager config,
        GetConfigurationResponse cfg,
        string prefix,
        string serviceName)
    {
        foreach (var item in cfg.Items.Where(k => k.Key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            var cleanKey = item.Key
                .Replace($"jobboard:config:{serviceName}:", "")
                .Replace("jobboard:config:global:", "");
            config[cleanKey] = item.Value.Value;
        }
    }
}
