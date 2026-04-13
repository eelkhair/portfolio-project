using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.DeleteEmbedding;

public class DeleteResumeEmbeddingCommand(EventDto<ResumeDeletedV1Event> @event) : BaseCommand<Unit>, ISystemCommand
{
    public EventDto<ResumeDeletedV1Event> Event { get; set; } = @event;
}

public class DeleteResumeEmbeddingCommandHandler(
    IHandlerContext context,
    IAiDbContext dbContext,
    IActivityFactory activityFactory) : BaseCommandHandler(context),
    IHandler<DeleteResumeEmbeddingCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteResumeEmbeddingCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("DeleteResumeEmbeddingCommandHandler.HandleAsync", ActivityKind.Internal);

        var resumeUId = request.Event.Data.ResumeUId;
        activity?.SetTag("resume.uid", resumeUId);

        Logger.LogInformation("Processing resume embedding deletion for {ResumeUId}", resumeUId);

        var existing = await dbContext.ResumeEmbeddings
            .FirstOrDefaultAsync(e => e.ResumeUId == resumeUId, cancellationToken);

        if (existing is null)
        {
            Logger.LogInformation("No embedding found for resume {ResumeUId}, nothing to delete", resumeUId);
            activity?.SetTag("embedding.found", false);
            return Unit.Value;
        }

        dbContext.ResumeEmbeddings.Remove(existing);

        // Delete associated match explanations
        var explanations = await dbContext.MatchExplanations
            .Where(e => e.ResumeUId == resumeUId)
            .ToListAsync(cancellationToken);
        dbContext.MatchExplanations.RemoveRange(explanations);

        await dbContext.SaveChangesAsync(cancellationToken);

        activity?.SetTag("embedding.found", true);
        activity?.SetTag("explanations.deleted", explanations.Count);
        Logger.LogInformation("Deleted embedding and {ExplanationCount} match explanations for resume {ResumeUId}",
            explanations.Count, resumeUId);

        return Unit.Value;
    }
}
