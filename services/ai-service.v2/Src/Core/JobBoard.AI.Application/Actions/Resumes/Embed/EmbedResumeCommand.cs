using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.Embed;

public class EmbedResumeCommand(EventDto<ResumeParsedEvent> @event) : BaseCommand<Unit>, ISystemCommand
{
    public EventDto<ResumeParsedEvent> Event { get; set; } = @event;
}

public class EmbedResumeCommandHandler(
    IHandlerContext context,
    IMonolithApiClient monolithClient,
    IAiDbContext dbContext,
    IEmbeddingService embeddingService,
    IActivityFactory activityFactory) : BaseCommandHandler(context),
    IHandler<EmbedResumeCommand, Unit>
{
    public async Task<Unit> HandleAsync(EmbedResumeCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("EmbedResumeCommandHandler.HandleAsync", ActivityKind.Internal);

        var resumeUId = request.Event.Data.ResumeUId;
        activity?.SetTag("resume.uid", resumeUId);

        Logger.LogInformation("Processing resume embedding for {ResumeUId}", resumeUId);

        var parsedContent = await monolithClient.GetResumeParsedContentAsync(resumeUId, cancellationToken);

        if (parsedContent is null)
        {
            Logger.LogWarning("No parsed content found for resume {ResumeUId}, skipping embedding", resumeUId);
            return Unit.Value;
        }

        // TODO: build embedding text from parsed content (skills, work history, education, etc.)
        // TODO: generate embedding vector via embeddingService.GenerateEmbeddingsAsync()
        // TODO: create ResumeEmbedding entity and store in dbContext
        // TODO: await dbContext.SaveChangesAsync(cancellationToken)

        Logger.LogInformation("Resume embedding stub completed for {ResumeUId}", resumeUId);

        return Unit.Value;
    }
}
