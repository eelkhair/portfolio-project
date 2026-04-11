using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.MatchExplanations;

public record GenerateAllMatchExplanationsResponse(int ResumesProcessed, int ExplanationsGenerated);

public class GenerateAllMatchExplanationsCommand : BaseCommand<GenerateAllMatchExplanationsResponse>, ISystemCommand;

public class GenerateAllMatchExplanationsCommandHandler(
    IHandlerContext context,
    IAiDbContext dbContext,
    IActivityFactory activityFactory,
    IApplicationOrchestrator orchestrator)
    : BaseCommandHandler(context),
        IHandler<GenerateAllMatchExplanationsCommand, GenerateAllMatchExplanationsResponse>
{
    private const int MaxConcurrency = 3;

    public async Task<GenerateAllMatchExplanationsResponse> HandleAsync(
        GenerateAllMatchExplanationsCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "GenerateAllMatchExplanationsCommandHandler.HandleAsync", ActivityKind.Internal);

        var resumeUIds = await dbContext.ResumeEmbeddings
            .Select(e => e.ResumeUId)
            .ToListAsync(cancellationToken);

        activity?.SetTag("resumes.total", resumeUIds.Count);
        Logger.LogInformation("Generating match explanations for {Count} resumes (concurrency: {Concurrency})",
            resumeUIds.Count, MaxConcurrency);

        var resumesProcessed = 0;
        var semaphore = new SemaphoreSlim(MaxConcurrency);

        var tasks = resumeUIds.Select(async resumeUId =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await orchestrator.ExecuteCommandAsync(
                    new GenerateMatchExplanationsCommand(resumeUId), cancellationToken);

                Interlocked.Increment(ref resumesProcessed);

                Logger.LogInformation(
                    "Generated explanations for resume {ResumeUId} ({Current}/{Total})",
                    resumeUId, resumesProcessed, resumeUIds.Count);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Failed to generate explanations for resume {ResumeUId}, continuing with next",
                    resumeUId);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var totalGenerated = await dbContext.MatchExplanations.CountAsync(cancellationToken);

        activity?.SetTag("resumes.processed", resumesProcessed);
        activity?.SetTag("explanations.total", totalGenerated);

        Logger.LogInformation(
            "Completed match explanation generation: {Processed}/{Total} resumes, {Explanations} explanations",
            resumesProcessed, resumeUIds.Count, totalGenerated);

        return new GenerateAllMatchExplanationsResponse(resumesProcessed, totalGenerated);
    }
}
