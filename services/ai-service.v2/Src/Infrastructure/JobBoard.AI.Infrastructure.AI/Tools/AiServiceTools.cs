using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Actions.Drafts.Save;
using JobBoard.AI.Application.Infrastructure.AI;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Tools;

public class AiServiceTools(
    IServiceProvider serviceProvider,
    IActivityFactory activityFactory,
    IToolExecutionCache cache
) : IAiTools
{
    private static readonly TimeSpan ToolTtl = TimeSpan.FromHours(1);

    public IEnumerable<AITool> GetTools()
    {
        yield return SaveDraftAiTool();
        yield return ListDraftsAiTool();

        yield return AIFunctionFactory.Create(
            (string input) =>
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["texas"] = "TX",
                    ["tx"] = "TX",
                    ["california"] = "CA",
                    ["ca"] = "CA"
                };

                return map.TryGetValue(input.Trim(), out var state)
                    ? state
                    : null;
            },
            new AIFunctionFactoryOptions
            {
                Name = "normalize_state",
                Description =
                    """
                    Converts a US state name or abbreviation into a 2-letter uppercase state code.
                    Returns null if unknown.
                    """
            });
    }



    private AITool ListDraftsAiTool()
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.draft_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "draft_list");
                activity?.AddTag("tool.company_id", companyId);

                var cacheKey = $"draft_list:{companyId}";

                if (cache.TryGet(cacheKey, out var cachedObj))
                {
                    var entry = (ToolCacheEntry)cachedObj!;
                    var age = DateTimeOffset.UtcNow - entry.ExecutedAt;

                    if (age < ToolTtl)
                    {
                        activity?.SetTag("tool.cache.hit", true);
                        activity?.SetTag("tool.cache.age_minutes", age.TotalMinutes);
                        return (ToolResultEnvelope<List<DraftResponse>>)entry.Value;
                    }

                    activity?.SetTag("tool.cache.expired", true);
                }

                activity?.SetTag("tool.cache.hit", false);

                var handler =
                    serviceProvider.GetRequiredService<
                        IHandler<ListDraftsQuery, List<DraftResponse>>>();

                var drafts = await handler.HandleAsync(
                    new ListDraftsQuery(companyId),
                    ct);

                var envelope = new ToolResultEnvelope<List<DraftResponse>>(
                    drafts,
                    DateTimeOffset.UtcNow);

                cache.Set(
                    cacheKey,
                    new ToolCacheEntry(envelope, envelope.ExecutedAt));

                activity?.SetTag("tool.result.count", drafts.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "draft_list",
                Description =
                    """
                    Returns a list of drafts for a company.
                    Requires companyId.

                    The AI may freely filter, count, group, or transform the returned drafts
                    in-memory without additional tools.
                    """
            });
    }

    private AITool SaveDraftAiTool()
    {
        return AIFunctionFactory.Create(
            async (SaveDraftCommand cmd, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.save_draft",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "save_draft");
                activity?.AddTag("tool.company_id", cmd.CompanyId);

                var handler =
                    serviceProvider.GetRequiredService<
                        IHandler<SaveDraftCommand, SaveDraftResponse>>();

                return await handler.HandleAsync(cmd, ct);
            },
            new AIFunctionFactoryOptions
            {
                Name = "save_draft",
                Description = "Saves a draft for a company."
            });
    }
}
