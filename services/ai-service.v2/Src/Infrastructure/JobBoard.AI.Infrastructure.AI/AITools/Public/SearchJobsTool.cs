using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Jobs.Search;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Public;

public static class SearchJobsTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IAiToolHandlerResolver toolResolver,
        IMonolithApiClient monolithClient,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("Search query describing the desired job role, e.g. 'React developer', 'data engineer', 'backend .NET'")] string query,
                [Description("Preferred job location, e.g. 'Austin, TX', 'remote', 'New York'")] string? location,
                [Description("Job type filter, e.g. 'FullTime', 'PartTime', 'Contract', 'Remote'")] string? jobType,
                [Description("Maximum number of results to return (default 10)")] int? limit,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.search_jobs", ActivityKind.Internal);
                activity?.SetTag("ai.operation", "search_jobs");
                activity?.SetTag("search.query", query);

                var effectiveLimit = limit ?? 10;
                var searchQuery = new SearchJobsQuery(query, location, jobType, effectiveLimit);
                var handler = toolResolver.Resolve<SearchJobsQuery, List<JobCandidate>>();
                var results = await handler.HandleAsync(searchQuery, ct);

                if (results.Count == 0)
                    return JsonSerializer.Serialize(new { message = "No jobs found matching your search. Try broader keywords or different filters." });

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
                        AboutRole = detail?.AboutRole,
                        r.Rank
                    };
                }).ToList();

                activity?.SetTag("search.count", enriched.Count);
                return JsonSerializer.Serialize(enriched);
            },
            new AIFunctionFactoryOptions
            {
                Name = "search_jobs",
                Description =
                    "Search for jobs by keywords, location, or job type using semantic search. " +
                    "Use when the user searches for specific roles like 'React developer in Austin' or 'remote data engineer'. " +
                    "Do NOT use this for resume-based matching — use find_matching_jobs instead."
            });
    }
}
