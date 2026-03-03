using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;

namespace JobBoard.Domain.Entities.Users;

public enum ResumeParseStatus
{
    Pending = 0,
    Processing = 1,
    Parsed = 2,
    Failed = 3
}

public class Resume : BaseAuditableEntity
{
    protected Resume()
    {
        FileName = string.Empty;
        OriginalFileName = string.Empty;
    }

    public int UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string FileName { get; private set; }
    public string OriginalFileName { get; private set; }
    public string? ContentType { get; private set; }
    public long? FileSize { get; private set; }
    public string? ParsedContent { get; private set; }
    public ResumeParseStatus ParseStatus { get; private set; } = ResumeParseStatus.Pending;
    public int ParseRetryCount { get; private set; }

    internal void SetUser(int userId) => UserId = userId;

    public void MarkProcessing()
    {
        ParseStatus = ResumeParseStatus.Processing;
    }

    public void MarkParsed(string parsedContent)
    {
        ParsedContent = parsedContent;
        ParseStatus = ResumeParseStatus.Parsed;
    }

    public void MarkParseFailed()
    {
        ParseStatus = ResumeParseStatus.Failed;
        ParseRetryCount++;
    }

    public void ResetForRetry()
    {
        ParseStatus = ResumeParseStatus.Processing;
    }

    public static Resume Create(ResumeInput input)
    {
        var errors = new List<Error>();

        DomainGuard.AgainstInvalidId(input.UserId, "Resume.InvalidUserId", errors);
        DomainGuard.AgainstNullOrEmpty(input.FileName, "Resume.FileNameRequired", errors);
        DomainGuard.AgainstNullOrEmpty(input.OriginalFileName, "Resume.OriginalFileNameRequired", errors);

        if (errors.Count > 0)
            throw new DomainException("Resume.InvalidEntity", errors);

        var resume = new Resume
        {
            FileName = input.FileName.Trim(),
            OriginalFileName = input.OriginalFileName.Trim(),
            ContentType = input.ContentType?.Trim(),
            FileSize = input.FileSize,
            ParsedContent = input.ParsedContent
        };

        resume.SetUser(input.UserId);
        resume.InternalId = input.InternalId;
        resume.Id = input.UId;

        EntityFactory.ApplyAudit(resume, input.CreatedAt, input.CreatedBy);

        return resume;
    }
}
