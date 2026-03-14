using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins.System;

public static class SetModeTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        ISettingsService settingsService,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("true for monolith mode, false for microservices mode")] bool isMonolith,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.set_mode",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "set_mode");
                activity?.SetTag("mode.target", isMonolith ? "monolith" : "microservices");

                logger.LogInformation("Setting application mode to {Mode}", isMonolith ? "monolith" : "microservices");
                await settingsService.UpdateApplicationModeAsync(new ApplicationModeDto { IsMonolith = isMonolith });
                return JsonSerializer.Serialize(new { success = true, isMonolith });
            },
            new AIFunctionFactoryOptions
            {
                Name = "set_mode",
                Description =
                    "Sets the application mode to monolith or microservices based on the provided boolean flag. " +
                    "true = monolith, false = microservices. " +
                    "WARNING: This tool MUST ONLY be used when the user explicitly asks to change system mode."
            });
    }
}
