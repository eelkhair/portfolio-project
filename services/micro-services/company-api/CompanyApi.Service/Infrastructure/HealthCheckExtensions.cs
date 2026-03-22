using AH.Metadata.Domain.Constants;
using Dapr.Client;
using Elkhair.Dev.Common.Domain.Constants;
using JobBoard.HealthChecks;
using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CompanyApi.Infrastructure;

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

            // -- Database --
            .AddSqlServer(
                builder.Configuration.GetConnectionString("CompanyDbContext")
                ?? throw new InvalidOperationException("DB connection missing"),
                name: "Company Database Check",
                timeout: TimeSpan.FromSeconds(10),
                tags: ["database", "critical"])

            // -- Authentication --
            .AddKeycloak(o =>
            {
                o.Authority = builder.Configuration["Keycloak:Authority"]
                              ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
                o.ClientIds = ["angular-admin", "angular-public", "dapr-service-client", "swagger-client"];
            })

            // -- Dapr sidecar, state store, secret store, pub/sub --
            .AddDapr();

        // Dapr configuration stores — only in deployed environments (Azure App Configuration)
        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.AddHealthChecks()
                .AddDaprConfigurationStore("global", o => o.StoreName = "appconfig-global")
                .AddDaprConfigurationStore("company", o => o.StoreName = "appconfig-company-api");
        }
    }
}
