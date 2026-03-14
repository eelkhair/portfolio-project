using System.Diagnostics;
using AdminAPI.Contracts.Models.Jobs.Responses;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Micro.Drafts;

public static class ListDraftsTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
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

                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<JobDraftResponse>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.AddTag("tool.source", "admin-api");
                activity?.SetTag("tool.cache.hit", false);

                var response = await client.ListDraftsAsync(companyId, ct);
                var drafts = response.Data ?? [];

                var envelope = new ToolResultEnvelope<List<JobDraftResponse>>(
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
                    Returns a list of drafts for a company.
                    """
            });
    }
}
