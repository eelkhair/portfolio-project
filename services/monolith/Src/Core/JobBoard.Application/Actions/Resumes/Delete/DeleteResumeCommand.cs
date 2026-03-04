using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Storage;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.Delete;

public class DeleteResumeCommand(Guid resumeId) : BaseCommand<bool>
{
    public Guid ResumeId { get; set; } = resumeId;
}

public class DeleteResumeCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db,
    IBlobStorageService blobStorage)
    : BaseCommandHandler(context),
      IHandler<DeleteResumeCommand, bool>
{
    private const string ContainerName = "resumes";

    public async Task<bool> HandleAsync(DeleteResumeCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("DeleteResume", ActivityKind.Internal);

        activity?.SetTag("resume.resume_id", command.ResumeId.ToString());

        var user = await db.Users
            .FirstAsync(u => u.ExternalId == command.UserId, cancellationToken);

        activity?.SetTag("resume.user_uid", user.Id.ToString());

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId && r.UserId == user.InternalId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeId} not found for user {UserUId}", command.ResumeId, user.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Resume not found");
            throw new NotFoundException($"Resume {command.ResumeId} not found.");
        }

        Logger.LogInformation("Deleting resume {ResumeUId} for user {UserUId}", resume.Id, user.Id);

        await blobStorage.DeleteAsync(ContainerName, resume.FileName, cancellationToken);

        db.Resumes.Remove(resume);

        var integrationEvent = new ResumeDeletedV1Event(ResumeUId: resume.Id)
        {
            UserId = command.UserId
        };

        await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            activity?.SetTag("resume.deleted", true);
            Logger.LogInformation("Successfully deleted resume {ResumeUId} for user {UserUId}", resume.Id, user.Id);
            return Task.CompletedTask;
        });

        return true;
    }
}
