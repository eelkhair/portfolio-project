using System.Diagnostics;
using System.Text.Json;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Storage;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities.Users;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.Upload;

public class UploadResumeCommand(Stream fileStream, string originalFileName, string contentType, long fileSize)
    : BaseCommand<ResumeResponse>
{
    public Stream FileStream { get; set; } = fileStream;
    public string OriginalFileName { get; set; } = originalFileName;
    public string ContentType { get; set; } = contentType;
    public long FileSize { get; set; } = fileSize;
}

public class UploadResumeCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db,
    IBlobStorageService blobStorage,
    IAiServiceClient aiServiceClient)
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

        await db.Resumes.AddAsync(resume, cancellationToken);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        // Parse resume via AI service
        var parsedContent = await TryParseResumeAsync(resume, blobName, command, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            activity?.SetTag("resume.resume_uid", resume.Id.ToString());
            activity?.SetTag("resume.file_size", command.FileSize);
            activity?.SetTag("resume.has_parsed_content", !string.IsNullOrEmpty(resume.ParsedContent));

            Logger.LogInformation(
                "Successfully uploaded resume {ResumeUId} for user {UserUId} ({FileName}, {FileSize} bytes, parsed={Parsed})",
                resume.Id, user.Id, command.OriginalFileName, command.FileSize,
                !string.IsNullOrEmpty(resume.ParsedContent));

            return Task.CompletedTask;
        });

        return new ResumeResponse
        {
            Id = resume.Id,
            OriginalFileName = resume.OriginalFileName,
            ContentType = resume.ContentType,
            FileSize = resume.FileSize,
            HasParsedContent = !string.IsNullOrEmpty(resume.ParsedContent),
            ParseStatus = resume.ParseStatus.ToString(),
            ParseRetryCount = resume.ParseRetryCount,
            CreatedAt = resume.CreatedAt,
            ParsedContent = parsedContent
        };
    }

    private async Task<ResumeParsedContentResponse?> TryParseResumeAsync(
        Resume resume,
        string blobName,
        UploadResumeCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Requesting AI resume parsing for {FileName}", command.OriginalFileName);

            var blob = await blobStorage.DownloadAsync(ContainerName, blobName, cancellationToken);
            var parsed = await aiServiceClient.ParseResume(
                command.OriginalFileName,
                command.ContentType,
                blob.Content,
                cancellationToken);

            var json = JsonSerializer.Serialize(parsed, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            resume.MarkParsed(json);
            await Context.SaveChangesAsync(command.UserId, cancellationToken);

            Logger.LogInformation("AI resume parsing succeeded for {FileName}", command.OriginalFileName);
            return parsed;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "AI resume parsing failed for {FileName} — upload will proceed without parsed content",
                command.OriginalFileName);

            resume.MarkParseFailed();
            await Context.SaveChangesAsync(command.UserId, cancellationToken);
            return null;
        }
    }
}
