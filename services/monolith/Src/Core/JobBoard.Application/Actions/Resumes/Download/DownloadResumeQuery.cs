using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.Download;

public record ResumeDownloadResult(Stream Content, string ContentType, string OriginalFileName);

public class DownloadResumeQuery(Guid resumeId) : BaseQuery<ResumeDownloadResult>
{
    public Guid ResumeId { get; set; } = resumeId;
}

public class DownloadResumeQueryHandler(
    IJobBoardQueryDbContext context,
    IActivityFactory activityFactory,
    IBlobStorageService blobStorage,
    ILogger<DownloadResumeQueryHandler> logger)
    : BaseQueryHandler(context, logger),
      IHandler<DownloadResumeQuery, ResumeDownloadResult>
{
    private const string ContainerName = "resumes";

    public async Task<ResumeDownloadResult> HandleAsync(DownloadResumeQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("DownloadResume", ActivityKind.Internal);
        activity?.SetTag("resume.resume_id", request.ResumeId.ToString());

        Logger.LogInformation("Downloading resume {ResumeId} for user {UserId}",
            request.ResumeId, request.UserId);

        var user = await Context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == request.UserId, cancellationToken);

        if (user is null)
        {
            Logger.LogWarning("User not found for external ID {UserId}", request.UserId);
            activity?.SetStatus(ActivityStatusCode.Error, "User not found");
            throw new NotFoundException("User", request.UserId!);
        }

        activity?.SetTag("resume.user_uid", user.Id.ToString());

        var resume = await Context.Resumes
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId && r.UserId == user.InternalId,
                cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeId} not found for user {UserUId}",
                request.ResumeId, user.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Resume not found");
            throw new NotFoundException($"Resume {request.ResumeId} not found.");
        }

        activity?.SetTag("resume.file_name", resume.OriginalFileName);

        var blob = await blobStorage.DownloadAsync(ContainerName, resume.FileName, cancellationToken);

        Logger.LogInformation("Successfully retrieved resume {ResumeUId} for user {UserUId}",
            resume.Id, user.Id);

        return new ResumeDownloadResult(
            blob.Content,
            resume.ContentType ?? blob.ContentType,
            resume.OriginalFileName);
    }
}
