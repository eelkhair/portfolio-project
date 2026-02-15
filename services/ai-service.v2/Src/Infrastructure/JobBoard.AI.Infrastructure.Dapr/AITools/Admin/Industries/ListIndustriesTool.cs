using System.Diagnostics;
using CompanyAPI.Contracts.Models.Industries.Responses;
using JobBoard.AI.Application.Infrastructure.AI;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Industries;

public static class ListIndustriesTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client, IToolExecutionCache cache, IUserAccessor accessor, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.industry_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "industry_list");

                var cacheKey = $"industry_list:{accessor.UserId}";

                if (cache.TryGet(cacheKey, out var cachedObj))
                {
                    var entry = (ToolCacheEntry)cachedObj!;
                    var age = DateTimeOffset.UtcNow - entry.ExecutedAt;

                    if (age < toolTtl)
                    {
                        activity?.SetTag("tool.cache.hit", true);
                        activity?.SetTag("tool.cache.age_minutes", age.TotalMinutes);
                        return (ToolResultEnvelope<List<IndustryResponse>>)entry.Value;
                    }

                    activity?.SetTag("tool.cache.expired", true);
                }
                activity?.AddTag("tool.source", "monolith");
                activity?.AddTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.cache.hit", false);

                var industries = await client.ListIndustriesAsync(ct);

                var envelope = new ToolResultEnvelope<List<IndustryResponse>>(
                    industries.Data!,
                    industries.Data!.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(
                    cacheKey,
                    new ToolCacheEntry(envelope, envelope.ExecutedAt));

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