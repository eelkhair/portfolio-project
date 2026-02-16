using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Infrastructure;

public interface IChatOptionsFactory
{
    ChatOptions Create(bool allowTools);
}

public sealed class ChatOptionsFactory(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IActivityFactory activityFactory)
    : IChatOptionsFactory
{
    public ChatOptions Create(bool allowTools)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

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

        using var activity = activityFactory.StartActivity("ai.chat.options", ActivityKind.Internal);

        activity?.AddTag("ai.tools", isMonolith ? "Monolith,AI" : "Micro,AI");
        activity?.AddTag("ai.tools.topology.count", topologyTools.Count);
        activity?.AddTag("ai.tools.ai.count", aiTools.Count);
        activity?.SetTag("ai.tools.allowed", allowTools);
        activity?.SetTag("ai.tools.count", allowTools ? allTools.Count : 0);

        return new ChatOptions
        {
            Tools = allowTools ? allTools : [],
            MaxOutputTokens = 5000,
            Temperature = 1,
            ModelId = configuration["AIModel"]!
        };
    }
}


