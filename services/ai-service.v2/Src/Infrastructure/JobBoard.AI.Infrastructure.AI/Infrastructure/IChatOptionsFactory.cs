using System.Diagnostics;
using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Infrastructure;

public interface IChatOptionsFactory
{
    ChatOptions Create(IServiceProvider sp, ChatScope scope, string userMessage, string? conversationSummary);
}

public sealed class ChatOptionsFactory(
    IConfiguration configuration,
    IActivityFactory activityFactory,
    IToolGroupSelector toolGroupSelector)
    : IChatOptionsFactory
{
    /// <summary>
    /// Maps tool names to their group. Tools not in this map default to "core".
    /// </summary>
    private static readonly Dictionary<string, string> ToolGroupMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Company group
        ["company_list"] = "company",
        ["create_company"] = "company",
        ["update_company"] = "company",
        ["industry_list"] = "company",

        // Draft group
        ["draft_list"] = "draft",
        ["save_draft"] = "draft",
        ["delete_draft"] = "draft",
        ["drafts_by_company"] = "draft",
        ["generate_draft"] = "draft",

        // Job group
        ["job_list"] = "job",
        ["company_job_summaries"] = "job",
        ["create_job"] = "job",

        // Core (explicit for clarity — tools not in map also default to core)
        ["system_info"] = "core",
        ["set_mode"] = "core"
    };

    public ChatOptions Create(IServiceProvider sp, ChatScope scope, string userMessage,
        string? conversationSummary)
    {
        var allTools = ResolveTools(sp, scope);
        var activeGroups = toolGroupSelector.SelectGroups(userMessage, conversationSummary);

        var filteredTools = allTools
            .Where(t => activeGroups.Contains(GetToolGroup(t.Name)))
            .ToList();

        using var activity = activityFactory.StartActivity("ai.chat.options", ActivityKind.Internal);
        activity?.SetTag("ai.chat.scope", scope.ToString());
        activity?.SetTag("ai.tools.total", allTools.Count);
        activity?.SetTag("ai.tools.filtered", filteredTools.Count);
        activity?.SetTag("ai.tools.groups", string.Join(",", activeGroups));

        return new ChatOptions
        {
            Tools = filteredTools,
            MaxOutputTokens = 5000,
            Temperature = 1,
            ModelId = configuration["AIModel"]!
        };
    }

    private static string GetToolGroup(string toolName)
        => ToolGroupMap.GetValueOrDefault(toolName, "core");

    private List<AITool> ResolveTools(IServiceProvider sp, ChatScope scope)
    {
        switch (scope)
        {
            case ChatScope.Admin:
            {
                var isMonolith = configuration.GetValue<bool>("FeatureFlags:Monolith");

                var topologyTools =
                    sp.GetRequiredKeyedService<IAiTools>(isMonolith ? "admin-monolith" : "admin-micro")
                        .GetTools()
                        .ToList();

                var aiTools =
                    sp.GetRequiredKeyedService<IAiTools>("admin-ai")
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
            {
                var isMonolith = configuration.GetValue<bool>("FeatureFlags:Monolith");

                var topologyTools =
                    sp.GetRequiredKeyedService<IAiTools>(isMonolith ? "public-monolith" : "public-micro")
                        .GetTools()
                        .ToList();

                var aiTools =
                    sp.GetRequiredKeyedService<IAiTools>("public-ai")
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

            default:
                return [];
        }
    }
}
