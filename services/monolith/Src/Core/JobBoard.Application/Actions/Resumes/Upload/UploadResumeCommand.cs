using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Storage;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities.Users;
using JobBoard.IntegrationEvents.Resume;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.Upload;

public class UploadResumeCommand(Stream fileStream, string originalFileName, string contentType, long fileSize, string? currentPage = null)
    : BaseCommand<ResumeResponse>
{
    public Stream FileStream { get; set; } = fileStream;
    public string OriginalFileName { get; set; } = originalFileName;
    public string ContentType { get; set; } = contentType;
    public long FileSize { get; set; } = fileSize;
    public string? CurrentPage { get; set; } = currentPage;
}

public class UploadResumeCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db,
    IBlobStorageService blobStorage)
    : BaseCommandHandler(context),
      IHandler<UploadResumeCommand, ResumeResponse>
{
    private const string ContainerName = "resumes";

    public async Task<ResumeResponse> HandleAsync(UploadResumeCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("UploadResume", ActivityKind.Internal);

        var user = await db.Users
            .FirstAsync(u => u.ExternalId == command.UserId, cancellationToken);

        activity?.SetTag("resume.user_uid", user.Id.ToString());

        var (id, uid) = await Context.GetNextValueFromSequenceAsync(typeof(Resume), cancellationToken);

        var ext = Path.GetExtension(command.OriginalFileName);
        var blobName = $"{user.Id}/{uid}{ext}";

        activity?.SetTag("resume.blob_name", blobName);
        Logger.LogInformation("Uploading resume for user {UserUId}: {FileName}", user.Id, command.OriginalFileName);

        await blobStorage.UploadAsync(ContainerName, blobName, command.FileStream, command.ContentType,
            cancellationToken);

        var resume = Resume.Create(new ResumeInput
        {
            UserId = user.InternalId,
            FileName = blobName,
            OriginalFileName = command.OriginalFileName,
            ContentType = command.ContentType,
            FileSize = command.FileSize,
            InternalId = id,
            UId = uid,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = command.UserId
        });

        resume.MarkProcessing();

        await db.Resumes.AddAsync(resume, cancellationToken);

        var integrationEvent = new ResumeUploadedV1Event(
            ResumeUId: uid,
            FileName: blobName,
            OriginalFileName: command.OriginalFileName,
            ContentType: command.ContentType,
            CurrentPage: command.CurrentPage)
        {
            UserId = command.UserId
        };

        await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            activity?.SetTag("resume.resume_uid", resume.Id.ToString());
            activity?.SetTag("resume.file_size", command.FileSize);

            Logger.LogInformation(
                "Uploaded resume {ResumeUId} for user {UserUId} ({FileName}, {FileSize} bytes) — parsing queued",
                resume.Id, user.Id, command.OriginalFileName, command.FileSize);

            return Task.CompletedTask;
        });

        return new ResumeResponse
        {
            Id = resume.Id,
            OriginalFileName = resume.OriginalFileName,
            ContentType = resume.ContentType,
            FileSize = resume.FileSize,
            HasParsedContent = false,
            ParseStatus = resume.ParseStatus.ToString(),
            ParseRetryCount = resume.ParseRetryCount,
            CreatedAt = resume.CreatedAt
        };
    }
}
