using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.AITools.Shared;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;

public static class ListCompanyJobSummariesTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.company_job_summaries",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "company_job_summaries");

                var cacheKey = $"company_job_summaries:{conversation.ConversationId}";
                activity?.SetTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.ttl.seconds", toolTtl.TotalSeconds);

                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<CompanyJobSummaryDto>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.AddTag("tool.source", "monolith");
                activity?.SetTag("tool.cache.hit", false);

                var summaries = await client.ListCompanyJobSummariesAsync(ct);

                var envelope = new ToolResultEnvelope<List<CompanyJobSummaryDto>>(
                    summaries,
                    summaries.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", summaries.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "company_job_summaries",
                Description =
                    "Returns all companies with their published jobs (title, location, type, salary range, date) and job count in a single call. ALWAYS use this tool first — it provides both summary counts AND full job listings for every company. Only use job_list if you need additional job details like responsibilities or qualifications."
            });
    }
}
