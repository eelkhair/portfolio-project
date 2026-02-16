using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.DraftsByCompany;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class DraftsByCompanyTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
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
               
                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<DraftsByCompanyResponse>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.SetTag("tool.cache.hit", false);

                var handler = toolResolver.Resolve<DraftsByCompanyQuery, DraftsByCompanyResponse>();

                var results = await handler.HandleAsync(new DraftsByCompanyQuery(), ct);

                var envelope = new ToolResultEnvelope<DraftsByCompanyResponse>(
                    results,
                    results.DraftsByCompany.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", results.DraftsByCompany.Count);

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