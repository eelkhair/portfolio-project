using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using JobAPI.Contracts.Models.Jobs.Responses;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Jobs;

public static class ListJobsTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
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

                activity?.AddTag("tool.source", "monolith");
                activity?.SetTag("tool.cache.hit", false);

                var jobs = await client.ListJobsAsync(companyId, ct);

                var envelope = new ToolResultEnvelope<List<JobResponse>>(
                    jobs.Value,
                    jobs.Value.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", jobs.Value.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "job_list",
                Description =
                    "Returns a list of published jobs for a company. Includes title, location, job type, salary range, and creation date."
            });
    }
}
