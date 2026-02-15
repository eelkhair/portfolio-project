using System.Diagnostics;
using JobBoard.AI.Application.Infrastructure.AI;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;

public static class ListCompaniesTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client, IToolExecutionCache cache, IUserAccessor accessor, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.company_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "company_list");

                var cacheKey = $"company_list:{accessor.UserId}";

                if (cache.TryGet(cacheKey, out var cachedObj))
                {
                    var entry = (ToolCacheEntry)cachedObj!;
                    var age = DateTimeOffset.UtcNow - entry.ExecutedAt;

                    if (age < toolTtl)
                    {
                        activity?.SetTag("tool.cache.hit", true);
                        activity?.SetTag("tool.cache.age_minutes", age.TotalMinutes);
                        return (ToolResultEnvelope<List<CompanyDto>>)entry.Value;
                    }

                    activity?.SetTag("tool.cache.expired", true);
                }
                activity?.AddTag("tool.source", "monolith");
                activity?.AddTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.cache.hit", false);

                var companies = await client.ListCompaniesAsync(ct);

                var envelope = new ToolResultEnvelope<List<CompanyDto>>(
                    companies.Value,
                    companies.Value.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(
                    cacheKey,
                    new ToolCacheEntry(envelope, envelope.ExecutedAt));

                activity?.SetTag("tool.result.count", companies.Value.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "company_list",
                Description =
                    "Returns a list all companies in the system."
            });

    }
}