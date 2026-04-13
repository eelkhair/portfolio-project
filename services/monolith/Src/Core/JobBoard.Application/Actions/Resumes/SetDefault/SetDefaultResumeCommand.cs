using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.SetDefault;

public class SetDefaultResumeCommand(Guid resumeId) : BaseCommand<bool>
{
    public Guid ResumeId { get; set; } = resumeId;
}

public class SetDefaultResumeCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<SetDefaultResumeCommand, bool>
{
    public async Task<bool> HandleAsync(SetDefaultResumeCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("SetDefaultResume", ActivityKind.Internal);

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

        var currentDefaults = await db.Resumes
            .Where(r => r.UserId == user.InternalId && r.IsDefault && r.InternalId != resume.InternalId)
            .ToListAsync(cancellationToken);

        foreach (var r in currentDefaults)
            r.ClearDefault();

        resume.SetAsDefault();

        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Logger.LogInformation("Set resume {ResumeUId} as default for user {UserUId}", resume.Id, user.Id);

        return true;
    }
}
