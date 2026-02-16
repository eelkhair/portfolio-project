using System.Diagnostics;
using CompanyAPI.Contracts.Models.Industries.Responses;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Industries;

public static class ListIndustriesTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
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
                
                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<IndustryResponse>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.AddTag("tool.source", "monolith");
                activity?.AddTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.cache.hit", false);

                var industries = await client.ListIndustriesAsync(ct);

                var envelope = new ToolResultEnvelope<List<IndustryResponse>>(
                    industries.Data!,
                    industries.Data!.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", industries.Data!.Count);

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
