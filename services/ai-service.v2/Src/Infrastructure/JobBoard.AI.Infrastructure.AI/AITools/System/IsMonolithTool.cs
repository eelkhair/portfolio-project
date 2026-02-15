using System.Diagnostics;
using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class IsMonolithTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IRedisStore store, ILogger<AiToolRegistry> logger)
    {
        return AIFunctionFactory.Create(async (CancellationToken cancellationToken) =>
        {
            using var activity = activityFactory.StartActivity("tool.is_monolith", ActivityKind.Internal);
            activity?.SetTag("ai.operation", "is_monolith");
            logger.LogInformation("Checking if monolith or microservice");
            var is_monolith = await store.GetAsync<string>("jobboard:config:global:FeatureFlags:Monolith", 1);
            activity?.SetTag("is_monolith", is_monolith);
            return is_monolith == "true";
        }, new AIFunctionFactoryOptions
        {
            Name="is_monolith",
            Description = "Checks if the application is running in monolith or microservices mode based on feature flag configuration. monolith = true, microservices = false"
        });
    }
}