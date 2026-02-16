using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class ProviderRetrievalTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver resolver, ILogger<AiToolRegistry> logger)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken cancellationToken) =>
                await ToolHelper.ExecuteAsync(activityFactory, "provider_retrieval",
                    async (activity, token) =>
                    {
                        logger.LogInformation("Retrieving provider");
                        var providerQuery = resolver.Resolve<GetProviderQuery, GetProviderResponse>();
                        var provider = await providerQuery.HandleAsync(new GetProviderQuery(), token);
                        logger.LogInformation("Provider retrieved: {Provider}, {Model}", provider.Provider, provider.Model);
                        activity?.SetTag("ai.provider", provider.Provider);
                        activity?.SetTag("ai.model", provider.Model);
                        return provider;
                    }, cancellationToken),
            new AIFunctionFactoryOptions
            {
                Name = "provider_retrieval",
                Description = "Retrieves the provider and model used for the current conversation."
            });
    }
}
