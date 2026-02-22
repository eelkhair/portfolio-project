using System.Diagnostics;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class SetModeTool
{
    public static AIFunction Get(IActivityFactory activityFactory, ISettingsService settingsService, ILogger<AiToolRegistry> logger)
    {
        return AIFunctionFactory.Create(async (bool isMonolith) =>
        {
            using var activity = activityFactory.StartActivity("tool.set_mode", ActivityKind.Internal);
            
            activity?.SetTag("ai.operation", "set_mode");
            logger.LogInformation("Setting application mode to {Mode}", isMonolith ? "monolith" : "microservices");
            
            await settingsService.UpdateApplicationModeAsync(new ApplicationModeDto{IsMonolith = isMonolith});
            activity?.SetTag("is_monolith", isMonolith);
            
        }, new AIFunctionFactoryOptions
        {
            Name="set_mode",
            Description = """
                          Sets the application mode to monolith or microservices based on the provided boolean flag. true = monolith, false = microservices
                          ⚠️ WARNING:
                          - This tool MUST ONLY be used when the user explicitly asks to change system mode.
                          - DO NOT call this tool unless the user clearly requests a mode change.
                        
                          """
        });
    }
}