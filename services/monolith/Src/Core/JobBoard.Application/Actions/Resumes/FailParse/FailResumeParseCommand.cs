using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.IntegrationEvents.Resume;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.FailParse;

public class FailResumeParseCommand(ResumeParseFailedModel model) : BaseCommand<ResumeParseFailureResult>
{
    public ResumeParseFailedModel Model { get; } = model;
}

public record ResumeParseFailureResult(int Attempt, int MaxAttempts, bool IsFinal);

public class FailResumeParseCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<FailResumeParseCommand, ResumeParseFailureResult>
{
    private const int MaxRetries = 3;

    public async Task<ResumeParseFailureResult> HandleAsync(FailResumeParseCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("FailResumeParse", ActivityKind.Internal);

        var model = command.Model;
        activity?.SetTag("resume.uid", model.ResumeUId);
        activity?.SetTag("resume.parse.failure_reason", model.Reason);

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == model.ResumeUId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeUId} not found for parse failure", model.ResumeUId);
            throw new NotFoundException($"Resume {model.ResumeUId} not found.");
        }

        if (resume.ParseStatus == Domain.Entities.Users.ResumeParseStatus.Parsed)
        {
            Logger.LogInformation(
                "Resume {ResumeUId} already parsed successfully, ignoring failure notification",
                model.ResumeUId);
            return new ResumeParseFailureResult(resume.ParseRetryCount, MaxRetries, IsFinal: false);
        }

        resume.MarkParseFailed();

        var isFinal = resume.ParseRetryCount >= MaxRetries;
        activity?.SetTag("resume.parse.retry_count", resume.ParseRetryCount);
        activity?.SetTag("resume.parse.is_final", isFinal);

        if (!isFinal)
        {
            resume.ResetForRetry();

            var retryEvent = new ResumeUploadedV1Event(
                ResumeUId: model.ResumeUId,
                FileName: resume.FileName,
                OriginalFileName: resume.OriginalFileName,
                ContentType: resume.ContentType ?? "application/octet-stream",
                CurrentPage: model.CurrentPage)
            {
                UserId = command.UserId
            };

            await OutboxPublisher.PublishAsync(retryEvent, cancellationToken);

            Logger.LogInformation(
                "Resume {ResumeUId} parse failed (attempt {Attempt}/{MaxRetries}), retrying: {Reason}",
                model.ResumeUId, resume.ParseRetryCount, MaxRetries, model.Reason);
        }
        else
        {
            Logger.LogWarning(
                "Resume {ResumeUId} parse permanently failed after {MaxRetries} attempts: {Reason}",
                model.ResumeUId, MaxRetries, model.Reason);
        }

        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        return new ResumeParseFailureResult(resume.ParseRetryCount, MaxRetries, isFinal);
    }
}
