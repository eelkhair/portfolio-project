using System.Diagnostics;
using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Infrastructure;

public interface IChatOptionsFactory
{
    ChatOptions Create(IServiceProvider sp, ChatScope scope);
}

public sealed class ChatOptionsFactory(
    IConfiguration configuration,
    IActivityFactory activityFactory)
    : IChatOptionsFactory
{
    public ChatOptions Create(IServiceProvider sp, ChatScope scope)
    {
        var tools = ResolveTools(sp, scope);

        using var activity = activityFactory.StartActivity("ai.chat.options", ActivityKind.Internal);
        activity?.SetTag("ai.chat.scope", scope.ToString());
        activity?.SetTag("ai.tools.count", tools.Count);

        return new ChatOptions
        {
            Tools = tools,
            MaxOutputTokens = 5000,
            Temperature = 1,
            ModelId = configuration["AIModel"]!
        };
    }

    private List<AITool> ResolveTools(IServiceProvider sp, ChatScope scope)
    {
        switch (scope)
        {
            case ChatScope.Admin:
            {
                var isMonolith = configuration.GetValue<bool>("FeatureFlags:Monolith");

                var topologyTools =
                    sp.GetRequiredKeyedService<IAiTools>(isMonolith ? "monolith" : "micro")
                        .GetTools()
                        .ToList();

                var aiTools =
                    sp.GetRequiredKeyedService<IAiTools>("ai")
                        .GetTools()
                        .ToList();

                var allTools = topologyTools.Concat(aiTools).ToList();

                var duplicates = allTools
                    .GroupBy(t => t.Name)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicates.Any())
                    throw new InvalidOperationException(
                        $"Duplicate AI tools detected: {string.Join(", ", duplicates)}");

                return allTools;
            }

            case ChatScope.CompanyAdmin:
                return sp.GetRequiredKeyedService<IAiTools>("company-admin")
                    .GetTools()
                    .ToList();

            case ChatScope.Public:
                return sp.GetRequiredKeyedService<IAiTools>("public")
                    .GetTools()
                    .ToList();

            default:
                return [];
        }
    }
}
