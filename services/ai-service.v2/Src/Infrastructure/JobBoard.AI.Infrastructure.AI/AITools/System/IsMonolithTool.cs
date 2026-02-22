using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class IsMonolithTool
{
    public static AIFunction Get(IActivityFactory activityFactory, ISettingsService settingsService, ILogger<AiToolRegistry> logger)
    {
        return AIFunctionFactory.Create(async () =>
        {
            using var activity = activityFactory.StartActivity("tool.is_monolith", ActivityKind.Internal);
            activity?.SetTag("ai.operation", "is_monolith");
            logger.LogInformation("Checking if monolith or microservice");
            var applicationMode = await settingsService.GetApplicationModeAsync();
            activity?.SetTag("is_monolith", applicationMode.IsMonolith);
            return applicationMode.IsMonolith;
        }, new AIFunctionFactoryOptions
        {
            Name="is_monolith",
            Description = """
                          Checks if the application is running in monolith or microservices mode based on feature flag configuration. monolith = true, microservices = false
                          MUST be returned if the user asks about system configuration
                          """
        });
    }
}