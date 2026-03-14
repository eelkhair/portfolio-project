using AH.Metadata.Domain.Constants;
using Dapr.Client;
using Elkhair.Dev.Common.Domain.Constants;
using JobBoard.HealthChecks;
using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdminApi.Infrastructure;

internal static class HealthCheckExtensions
{
    public static void AddCustomHealthChecks(this WebApplicationBuilder builder)
    {
        // --- Dapr infrastructure singletons ---
        var stateStore = new StateStoreOptions()
        {
            StoreName = StateStores.Redis
        };
        builder.Services.AddSingleton(_ => new DaprStateStoreHealthCheck(new DaprClientBuilder().Build(), stateStore));

        var secretStore = new SecretStoreOptions
        {
            StoreName = SecretStoreNames.Local
        };
        builder.Services.AddSingleton(_ => new DaprSecretStoreHealthCheck(new DaprClientBuilder().Build(), secretStore));

        var pubSub = new DistributedEventBusOptions
        {
            Prefix = string.Empty,
            Postfix = string.Empty,
            PubSubName = PubSubNames.RabbitMq
        };

        builder.Services.AddSingleton(_ => new DaprPubSubHealthCheck(new DaprClientBuilder().Build(), pubSub));

        builder.Services.AddHttpClient();

        builder.Services
            .AddHealthChecks()

            // -- Liveness --
            .AddCheck("self", () => HealthCheckResult.Healthy())

            // -- Authentication --
            .AddKeycloak(o =>
            {
                o.Authority = builder.Configuration["Keycloak:Authority"]
                              ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
                o.ClientIds = ["angular-admin", "angular-public", "dapr-service-client", "swagger-client"];
            })

            // -- Dapr sidecar, state store, secret store, pub/sub --
            .AddDapr()

            // -- Dapr configuration stores --
            .AddDaprConfigurationStore("global", o =>
                o.StoreName = "appconfig-global")
            .AddDaprConfigurationStore("admin", o =>
                o.StoreName = "appconfig-admin-api")

            // -- MCP Server --
            .AddCheck("mcp-server", () =>
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var response = client.GetAsync("http://localhost:3334/").GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode
                        ? HealthCheckResult.Healthy("MCP server is reachable")
                        : HealthCheckResult.Degraded($"MCP server returned {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("MCP server unreachable", ex);
                }
            }, tags: ["mcp"]);
    }
}
