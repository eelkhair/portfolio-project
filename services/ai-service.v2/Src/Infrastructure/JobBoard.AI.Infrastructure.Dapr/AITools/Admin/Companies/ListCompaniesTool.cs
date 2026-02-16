using System.Diagnostics;
using CompanyAPI.Contracts.Models.Companies.Responses;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Companies;

public static class ListCompaniesTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.company_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "company_list");

                var cacheKey = $"company_list:{conversation.ConversationId}";
                activity?.SetTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.ttl.seconds", toolTtl.TotalSeconds);

                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<CompanyResponse>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.AddTag("tool.source", "microservices");
                activity?.AddTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.cache.hit", false);

                var companies = await client.ListCompaniesAsync(ct);

                var envelope = new ToolResultEnvelope<List<CompanyResponse>>(
                    companies.Data!,
                    companies.Data!.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", companies.Data!.Count);

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
