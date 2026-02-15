
using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Infrastructure.AI;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.Tools.Drafts;

public static class ListDraftsTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IToolExecutionCache cache, TimeSpan toolTtl)
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

                    if (age < toolTtl)
                    {
                        activity?.SetTag("tool.cache.hit", true);
                        activity?.SetTag("tool.cache.age_minutes", age.TotalMinutes);
                        return (ToolResultEnvelope<List<DraftResponse>>)entry.Value;
                    }

                    activity?.SetTag("tool.cache.expired", true);
                }

                activity?.SetTag("tool.cache.hit", false);

                var handler = toolResolver.Resolve<ListDraftsQuery, List<DraftResponse>>();

                var drafts = await handler.HandleAsync(
                    new ListDraftsQuery(companyId),
                    ct);

                var envelope = new ToolResultEnvelope<List<DraftResponse>>(
                    drafts,
                    drafts.Count,
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
                    "Returns a list of drafts for a company.
                    """
            });

    }
}

