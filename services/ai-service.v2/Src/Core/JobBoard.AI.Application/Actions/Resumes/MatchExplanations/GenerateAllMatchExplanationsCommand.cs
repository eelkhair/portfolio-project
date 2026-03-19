using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.MatchExplanations;

public record GenerateAllMatchExplanationsResponse(int ResumesProcessed, int ExplanationsGenerated);

public class GenerateAllMatchExplanationsCommand : BaseCommand<GenerateAllMatchExplanationsResponse>, ISystemCommand;

public class GenerateAllMatchExplanationsCommandHandler(
    IHandlerContext context,
    IAiDbContext dbContext,
    IActivityFactory activityFactory,
    IServiceScopeFactory serviceScopeFactory)
    : BaseCommandHandler(context),
        IHandler<GenerateAllMatchExplanationsCommand, GenerateAllMatchExplanationsResponse>
{
    public async Task<GenerateAllMatchExplanationsResponse> HandleAsync(
        GenerateAllMatchExplanationsCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "GenerateAllMatchExplanationsCommandHandler.HandleAsync", ActivityKind.Internal);

        var resumeEmbeddings = await dbContext.ResumeEmbeddings
            .Select(e => e.ResumeUId)
            .ToListAsync(cancellationToken);

        activity?.SetTag("resumes.total", resumeEmbeddings.Count);
        Logger.LogInformation("Generating match explanations for {Count} resumes", resumeEmbeddings.Count);

        var totalExplanations = 0;
        var resumesProcessed = 0;

        foreach (var resumeUId in resumeEmbeddings)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<IHandler<GenerateMatchExplanationsCommand, Unit>>();

                await handler.HandleAsync(
                    new GenerateMatchExplanationsCommand(resumeUId),
                    cancellationToken);

                var generated = await dbContext.MatchExplanations
                    .CountAsync(e => e.ResumeUId == resumeUId, cancellationToken);

                totalExplanations += generated;
                resumesProcessed++;

                Logger.LogInformation(
                    "Generated explanations for resume {ResumeUId} ({Current}/{Total})",
                    resumeUId, resumesProcessed, resumeEmbeddings.Count);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Failed to generate explanations for resume {ResumeUId}, continuing with next",
                    resumeUId);
            }
        }

        activity?.SetTag("resumes.processed", resumesProcessed);
        activity?.SetTag("explanations.total", totalExplanations);

        Logger.LogInformation(
            "Completed match explanation generation: {Processed}/{Total} resumes, {Explanations} explanations",
            resumesProcessed, resumeEmbeddings.Count, totalExplanations);

        return new GenerateAllMatchExplanationsResponse(resumesProcessed, totalExplanations);
    }
}
