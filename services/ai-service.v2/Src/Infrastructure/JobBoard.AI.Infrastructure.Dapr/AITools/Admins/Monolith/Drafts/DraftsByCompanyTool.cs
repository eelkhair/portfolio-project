using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Drafts;

public static class DraftsByCompanyTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.drafts_by_company",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "drafts_by_company");

                var cacheKey = $"drafts_by_company:{conversation.ConversationId}";
                activity?.SetTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.ttl.seconds", toolTtl.TotalSeconds);

                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<Dictionary<Guid, DraftsByCompanyItemResponse>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.AddTag("tool.source", "monolith");
                activity?.SetTag("tool.cache.hit", false);

                var results = await client.ListAllDraftsByCompanyAsync(ct);

                var envelope = new ToolResultEnvelope<Dictionary<Guid, DraftsByCompanyItemResponse>>(
                    results,
                    results.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", results.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "drafts_by_company",
                Description = """
                              Returns job drafts grouped by company.
                              Each company entry contains a list of drafts and a count.
                              """
            });
    }
}
