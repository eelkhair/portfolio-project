using System.Diagnostics;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Jobs;

public static class ListJobsTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.job_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "job_list");
                activity?.AddTag("tool.company_id", companyId);

                var cacheKey = $"job_list:{conversation.ConversationId}:{companyId}";
                activity?.SetTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.ttl.seconds", toolTtl.TotalSeconds);

                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<JobResponse>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.AddTag("tool.source", "microservices");
                activity?.SetTag("tool.cache.hit", false);

                var jobs = await client.ListJobsAsync(companyId, ct);

                var envelope = new ToolResultEnvelope<List<JobResponse>>(
                    jobs.Data!,
                    jobs.Data!.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", jobs.Data!.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "job_list",
                Description =
                    "Returns detailed published jobs for a single company including responsibilities, qualifications, and about role. Use only when you need these extra details beyond what company_job_summaries already provides."
            });
    }
}
