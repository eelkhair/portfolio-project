using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins.Drafts;

public static class ListDraftsTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.draft_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "draft_list");
                activity?.AddTag("tool.company_id", companyId);

                var cacheKey = $"draft_list:{conversation.ConversationId}:{companyId}";
                activity?.SetTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.ttl.seconds", toolTtl.TotalSeconds);

                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<DraftResponse>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
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

                cache.Set(cacheKey, envelope, toolTtl);

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
