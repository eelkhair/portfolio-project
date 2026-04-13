using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Entities.Users;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.ReEmbed;

public class ReEmbedResumeCommand(Guid resumeId) : BaseCommand<Unit>
{
    public Guid ResumeId { get; } = resumeId;
}

public class ReEmbedResumeCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<ReEmbedResumeCommand, Unit>
{
    public async Task<Unit> HandleAsync(ReEmbedResumeCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ReEmbedResume", ActivityKind.Internal);
        activity?.SetTag("resume.uid", command.ResumeId);

        var user = await db.Users
            .FirstAsync(u => u.ExternalId == command.UserId, cancellationToken);

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId && r.UserId == user.InternalId, cancellationToken);

        if (resume is null)
            throw new NotFoundException($"Resume {command.ResumeId} not found.");

        if (resume.ParseStatus is not (ResumeParseStatus.Parsed or ResumeParseStatus.PartiallyParsed))
            throw new InvalidOperationException("Resume must be parsed before it can be embedded.");

        var integrationEvent = new ResumeParsedV1Event(ResumeUId: resume.Id)
        {
            UserId = command.UserId
        };

        await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Logger.LogInformation("Re-embed requested for resume {ResumeUId}, publishing ResumeParsedV1Event", resume.Id);

        return Unit.Value;
    }
}
