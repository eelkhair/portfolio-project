using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobBoard.HealthChecks;

public static class DaprHealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddDapr(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<DaprHealthCheck>("dapr");
        builder.AddCheck<DaprStateStoreHealthCheck>("dapr.state");
        builder.AddCheck<DaprSecretStoreHealthCheck>("dapr.secret");
        builder.AddCheck<DaprPubSubHealthCheck>("dapr.pubsub");

        return builder;
    }

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
            tags: new[] { "dapr", "config" }
        ));

        return builder;
    }
}