using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Clients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Public;

public static class GetJobDetailTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IMonolithApiClient monolithClient,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("The job ID (GUID) to get details for")] Guid jobId,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.get_job_detail", ActivityKind.Internal);
                activity?.SetTag("ai.operation", "get_job_detail");
                activity?.SetTag("job.id", jobId);

                var jobs = await monolithClient.GetJobsBatchAsync([jobId], ct);
                var job = jobs.FirstOrDefault();

                if (job is null)
                    return JsonSerializer.Serialize(new { error = "Job not found. It may have been removed or the ID is incorrect." });

                return JsonSerializer.Serialize(new
                {
                    job.JobId,
                    job.Title,
                    job.AboutRole,
                    job.Location,
                    job.JobType,
                    job.SalaryRange,
                    job.Responsibilities,
                    job.Qualifications
                });
            },
            new AIFunctionFactoryOptions
            {
                Name = "get_job_detail",
                Description =
                    "Get full details of a specific job listing including title, description, responsibilities, qualifications, and salary. " +
                    "Use when the user asks about a particular job's requirements, salary, or wants to learn more about a specific position."
            });
    }
}
