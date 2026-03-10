using AH.Metadata.Domain.Constants;
using Dapr.Client;
using Elkhair.Dev.Common.Domain.Constants;
using JobBoard.AI.API.Infrastructure.HealthChecks;
using JobBoard.HealthChecks;
using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobBoard.AI.API.Infrastructure;

internal static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddCustomHealthChecks(this WebApplicationBuilder builder)
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

            // -- Database --
            .AddNpgSql(
                builder.Configuration.GetConnectionString("ai-db")
                ?? throw new InvalidOperationException("ai-db connection string missing"),
                name: "postgres",
                timeout: TimeSpan.FromSeconds(10),
                tags: ["database", "critical"])

            // -- AI Providers --
            .AddCheck<OpenAiHealthCheck>("openai", tags: ["ai"])
            .AddCheck<AzureOpenAiHealthCheck>("azure openai", tags: ["ai"])
            .AddCheck<AnthropicHealthCheck>("anthropic", tags: ["ai"])
            .AddCheck<GeminiHealthCheck>("gemini", tags: ["ai"])

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
            .AddDaprConfigurationStore("appconfig-ai-service-v2", o =>
                o.StoreName = "appconfig-ai-service-v2");

        return builder;
    }
}
