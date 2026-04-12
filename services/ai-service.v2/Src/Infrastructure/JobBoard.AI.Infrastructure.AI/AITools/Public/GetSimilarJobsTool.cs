using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Public;

public static class GetSimilarJobsTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IAiToolHandlerResolver toolResolver,
        IMonolithApiClient monolithClient,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("The job ID (GUID) to find similar jobs for")] Guid jobId,
                [Description("Maximum number of similar jobs to return (default 5)")] int? limit,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.get_similar_jobs", ActivityKind.Internal);
                activity?.SetTag("ai.operation", "get_similar_jobs");
                activity?.SetTag("job.id", jobId);

                var effectiveLimit = limit ?? 5;
                var query = new GetSimilarJobsQuery(jobId, effectiveLimit);
                var handler = toolResolver.Resolve<GetSimilarJobsQuery, List<JobCandidate>>();
                var results = await handler.HandleAsync(query, ct);

                if (results.Count == 0)
                    return JsonSerializer.Serialize(new { message = "No similar jobs found for this position." });

                // Enrich with job details
                var jobIds = results.Select(r => r.JobId).ToList();
                var jobDetails = await monolithClient.GetJobsBatchAsync(jobIds, ct);
                var jobMap = jobDetails.ToDictionary(j => j.JobId);

                var enriched = results.Select(r =>
                {
                    jobMap.TryGetValue(r.JobId, out var detail);
                    return new
                    {
                        r.JobId,
                        Title = detail?.Title ?? "Unknown",
                        Location = detail?.Location,
                        JobType = detail?.JobType,
                        SalaryRange = detail?.SalaryRange,
                        r.Similarity,
                        r.Rank
                    };
                }).ToList();

                activity?.SetTag("similar.count", enriched.Count);
                return JsonSerializer.Serialize(enriched);
            },
            new AIFunctionFactoryOptions
            {
                Name = "get_similar_jobs",
                Description =
                    "Find jobs similar to a specific job posting. " +
                    "Use when the user says 'show me jobs like this one', 'find similar positions', or wants alternatives to a specific job."
            });
    }
}
