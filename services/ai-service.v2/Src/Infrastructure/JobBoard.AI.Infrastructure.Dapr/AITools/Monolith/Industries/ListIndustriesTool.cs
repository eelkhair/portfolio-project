using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Industries;

public static class ListIndustriesTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.industry_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "industry_list");

                var cacheKey = $"industry_list:{conversation.ConversationId}";
                activity?.SetTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.ttl.seconds", toolTtl.TotalSeconds);

                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<IndustryDto>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.AddTag("tool.source", "monolith");
                activity?.AddTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.cache.hit", false);

                var industries = await client.ListIndustriesAsync(ct);

                var envelope = new ToolResultEnvelope<List<IndustryDto>>(
                    industries.Value,
                    industries.Value.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", industries.Value.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "industry_list",
                Description =
                    "Returns a list all industries in the system."
            });

    }
}
