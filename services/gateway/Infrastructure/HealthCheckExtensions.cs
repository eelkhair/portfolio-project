using AH.Metadata.Domain.Constants;
using Dapr.Client;
using Elkhair.Dev.Common.Domain.Constants;
using JobBoard.HealthChecks;
using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.Api.Infrastructure;

internal static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddCustomHealthChecks(this WebApplicationBuilder builder)
    {
        var stateStore = new StateStoreOptions
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
        
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddDapr()
            .AddDaprConfigurationStore("global", o =>
                o.StoreName = "appconfig-global")
            .AddDaprConfigurationStore("connector", o =>
                o.StoreName = "appconfig-gateway");

        return builder;
    }
}