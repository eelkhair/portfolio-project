using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins.System;

public static class IsMonolithTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        ISettingsService settingsService)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.is_monolith",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "is_monolith");

                var mode = await settingsService.GetApplicationModeAsync();
                return JsonSerializer.Serialize(new { isMonolith = mode.IsMonolith });
            },
            new AIFunctionFactoryOptions
            {
                Name = "is_monolith",
                Description =
                    "Checks if the application is running in monolith or microservices mode based on feature flag configuration. " +
                    "monolith = true, microservices = false. MUST be returned if the user asks about system configuration."
            });
    }
}
