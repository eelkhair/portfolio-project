using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobBoard.HealthChecks;

public static class DaprHealthCheckBuilderExtensions
{
    /// <summary>
    /// Adds Dapr sidecar, state store, secret store, and pub/sub health checks.
    /// </summary>
    public static IHealthChecksBuilder AddDapr(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<DaprHealthCheck>("dapr", tags: ["dapr"]);
        builder.AddCheck<DaprStateStoreHealthCheck>("dapr.state", tags: ["dapr"]);
        builder.AddCheck<DaprSecretStoreHealthCheck>("dapr.secret", tags: ["dapr"]);
        builder.AddCheck<DaprPubSubHealthCheck>("dapr.pubsub", tags: ["dapr"]);

        return builder;
    }

    /// <summary>
    /// Adds a Dapr configuration store health check with the specified name.
    /// </summary>
    public static IHealthChecksBuilder AddDaprConfigurationStore(
        this IHealthChecksBuilder builder,
        string name,
        Action<ConfigurationStoreOptions> configure)
    {
        builder.Services.Configure(name, configure);

        builder.Services.PostConfigure<ConfigurationStoreOptions>(name, options =>
        {
            if (string.IsNullOrWhiteSpace(options.StoreName))
                throw new InvalidOperationException(
                    $"Dapr configuration store '{name}' must define StoreName.");
        });

        builder.Add(new HealthCheckRegistration(
            name: $"dapr.config.{name}",
            factory: sp =>
            {
                var daprClient = sp.GetRequiredService<DaprClient>();
                var options = sp.GetRequiredService<IOptionsMonitor<ConfigurationStoreOptions>>();

                return new DaprConfigurationStoreHealthCheck(
                    daprClient,
                    options,
                    name);
            },
            failureStatus: HealthStatus.Unhealthy,
            tags: ["dapr", "config"]
        ));

        return builder;
    }

    /// <summary>
    /// Adds a Keycloak OIDC realm reachability health check.
    /// Validates the discovery endpoint and reports configured client IDs.
    /// </summary>
    public static IHealthChecksBuilder AddKeycloak(
        this IHealthChecksBuilder builder,
        Action<KeycloakOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.AddCheck<KeycloakHealthCheck>("keycloak", tags: ["auth"]);

        return builder;
    }
}
