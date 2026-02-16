using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class SetModeTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IRedisStore store, ILogger<AiToolRegistry> logger)
    {
        return AIFunctionFactory.Create(
            async (bool isMonolith) =>
                await ToolHelper.ExecuteAsync(activityFactory, 
                    "set_mode",
                    async activity =>
                    {
                        logger.LogInformation("Setting application mode to {Mode}", isMonolith ? "monolith" : "microservices");
                        await store.SetAsync("jobboard:config:global:FeatureFlags:Monolith", isMonolith ? "true" : "false", 1);
                        activity?.SetTag("is_monolith", isMonolith);
                    }),
            new AIFunctionFactoryOptions
            {
                Name = "set_mode",
                Description = "Sets the application mode to monolith or microservices based on the provided boolean flag. true = monolith, false = microservices"
            });
    }
}
