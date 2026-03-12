using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Jobs.ReEmbedAll;

public record ReEmbedAllJobsResponse(int JobsProcessed);

public class ReEmbedAllJobsCommand : BaseCommand<ReEmbedAllJobsResponse>, ISystemCommand;

public class ReEmbedAllJobsCommandHandler(
    IHandlerContext context,
    IAiDbContext dbContext,
    IMonolithApiClient monolithApiClient,
    IEmbeddingService embeddingService,
    IActivityFactory activityFactory)
    : BaseCommandHandler(context),
      IHandler<ReEmbedAllJobsCommand, ReEmbedAllJobsResponse>
{
    public async Task<ReEmbedAllJobsResponse> HandleAsync(
        ReEmbedAllJobsCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ReEmbedAllJobs", ActivityKind.Internal);

        var summaries = await monolithApiClient.ListCompanyJobSummariesAsync(cancellationToken);
        var companiesWithJobs = summaries.Where(s => s.JobCount > 0).ToList();

        activity?.SetTag("companies.count", companiesWithJobs.Count);
        Logger.LogInformation("Re-embedding jobs for {CompanyCount} companies", companiesWithJobs.Count);

        var totalProcessed = 0;

        foreach (var company in companiesWithJobs)
        {
            var jobs = await monolithApiClient.ListJobsAsync(company.CompanyId, cancellationToken);

            foreach (var job in jobs)
            {
                var embeddingText = BuildEmbeddingText(job);

                var vector = await embeddingService.GenerateEmbeddingsAsync(
                    embeddingText, cancellationToken);

                var existing = await dbContext.JobEmbeddings
                    .FirstOrDefaultAsync(e => e.JobId == job.Id, cancellationToken);

                if (existing is not null)
                    dbContext.JobEmbeddings.Remove(existing);

                var jobEmbedding = new JobEmbedding(
                    jobId: job.Id,
                    vector: new EmbeddingVector(vector),
                    provider: new ProviderName("openai.embedding"),
                    model: new ModelName("text-embedding-3-small"));

                await dbContext.JobEmbeddings.AddAsync(jobEmbedding, cancellationToken);
                totalProcessed++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            Logger.LogInformation(
                "Re-embedded {JobCount} jobs for company {CompanyName} ({CompanyId})",
                jobs.Count, company.CompanyName, company.CompanyId);
        }

        activity?.SetTag("jobs.processed", totalProcessed);
        Logger.LogInformation("Re-embed complete: {TotalProcessed} jobs processed", totalProcessed);

        return new ReEmbedAllJobsResponse(totalProcessed);
    }

    private static string BuildEmbeddingText(JobBoard.Monolith.Contracts.Jobs.JobResponse job)
    {
        return $"""
                Job Title: {job.Title}
                Location: {job.Location}
                Job Type: {job.JobType}

                About the Role:
                {job.AboutRole}

                Responsibilities:
                {string.Join("\n", job.Responsibilities)}

                Qualifications:
                {string.Join("\n", job.Qualifications)}
                """;
    }
}
