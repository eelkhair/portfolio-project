using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Actions.Resumes.MatchingJobs;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Public;

public static class FindMatchingJobsTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IAiToolHandlerResolver toolResolver,
        IAiDbContext dbContext,
        IMonolithApiClient monolithClient,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("Resume ID as a GUID. If omitted, the user's most recent resume embedding is used automatically.")] Guid? resumeId,
                [Description("Maximum number of matching jobs to return (default 10)")] int? limit,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.find_matching_jobs", ActivityKind.Internal);
                activity?.SetTag("ai.operation", "find_matching_jobs");

                try
                {
                    var effectiveLimit = limit ?? 10;

                    // Auto-resolve resume if not provided
                    Guid resolvedResumeId;
                    if (resumeId.HasValue)
                    {
                        resolvedResumeId = resumeId.Value;
                    }
                    else
                    {
                        var embedding = await dbContext.ResumeEmbeddings
                            .OrderByDescending(e => e.CreatedAt)
                            .FirstOrDefaultAsync(ct);

                        if (embedding is null)
                            return JsonSerializer.Serialize(new { error = "No resume found. Please upload a resume first to get job recommendations." });

                        resolvedResumeId = embedding.ResumeUId;
                        logger.LogInformation("Auto-resolved resume {ResumeId} for matching", resolvedResumeId);
                    }

                    activity?.SetTag("resume.uid", resolvedResumeId);

                    var query = new ListMatchingJobsQuery(resolvedResumeId, effectiveLimit);
                    var handler = toolResolver.Resolve<ListMatchingJobsQuery, List<JobCandidate>>();
                    var results = await handler.HandleAsync(query, ct);

                    if (results.Count == 0)
                        return JsonSerializer.Serialize(new { message = "No matching jobs found. Your resume may still be processing — try again in a moment." });

                    // Enrich with job details from monolith
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
                            MatchScore = $"{r.Similarity:P0}",
                            r.Rank,
                            r.MatchSummary,
                            r.MatchDetails,
                            r.MatchGaps
                        };
                    }).ToList();

                    activity?.SetTag("matching.count", enriched.Count);
                    return JsonSerializer.Serialize(enriched);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "FindMatchingJobsTool failed");
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", ex.Message);
                    return JsonSerializer.Serialize(new { error = $"Failed to find matching jobs: {ex.Message}" });
                }
            },
            new AIFunctionFactoryOptions
            {
                Name = "find_matching_jobs",
                Description =
                    "Find jobs that best match the user's resume using AI-powered semantic matching. " +
                    "ALWAYS call this tool immediately when the user asks for job recommendations, matching jobs, 'best jobs for me', or 'what jobs fit my profile'. " +
                    "Do NOT ask the user for their resume — the tool automatically finds it. Just call it with no parameters. " +
                    "Returns ranked results with match scores, explanations, and skill gaps."
            });
    }
}
