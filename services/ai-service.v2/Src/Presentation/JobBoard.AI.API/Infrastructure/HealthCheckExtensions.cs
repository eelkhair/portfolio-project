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
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddNpgSql(
                builder.Configuration.GetConnectionString("ai-db")
                ?? throw new InvalidOperationException("ai-db connection string missing"),
                name: "postgres",
                timeout: TimeSpan.FromSeconds(10),
                tags: ["database", "critical"])
            .AddCheck<OpenAiHealthCheck>("openai")
            .AddCheck<AzureOpenAiHealthCheck>("azure openai")
            .AddCheck<AnthropicHealthCheck>("anthropic")
            .AddCheck<GeminiHealthCheck>("gemini")
            .AddDapr()
            .AddDaprConfigurationStore("global", o =>
                o.StoreName = "appconfig-global")
            .AddDaprConfigurationStore("appconfig-ai-service-v2", o =>
                o.StoreName = "appconfig-ai-service-v2");

        return builder;
    }
}
