using System.Text.Json;
using System.Text.Json.Nodes;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;

namespace JobBoard.Domain.Entities.Users;

public enum ResumeParseStatus
{
    Pending = 0,
    Processing = 1,
    Parsed = 2,
    Failed = 3,
    PartiallyParsed = 4
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
    public string? ParsedSections { get; private set; }
    public string? FailedSections { get; private set; }
    public bool IsDefault { get; private set; }

    internal void SetUser(int userId) => UserId = userId;

    public void SetAsDefault() => IsDefault = true;

    public void ClearDefault() => IsDefault = false;

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

    private static readonly string[] RequiredSections = ["contact", "skills", "workHistory", "education", "certifications", "projects"];

    // Sections whose JSON properties are merged at the top level (flat fields, not wrapped arrays)
    private static readonly HashSet<string> TopLevelMergeSections = ["quick", "contact", "skills"];

    public void MergeSectionContent(string section, string sectionJson)
    {
        var existing = string.IsNullOrEmpty(ParsedContent)
            ? new JsonObject()
            : JsonNode.Parse(ParsedContent)!.AsObject();

        var sectionNode = JsonNode.Parse(sectionJson);

        if (TopLevelMergeSections.Contains(section))
        {
            // Flat sections (contact, skills, legacy quick) — merge each property at root level
            foreach (var prop in sectionNode!.AsObject())
            {
                existing.Remove(prop.Key);
                existing[prop.Key] = prop.Value?.DeepClone();
            }
        }
        else
        {
            // Section responses are wrapped in an object like { "WorkHistory": [...] }
            // Unwrap to get just the array value
            var contentNode = sectionNode;
            if (sectionNode is JsonObject obj && obj.Count == 1)
            {
                contentNode = obj.First().Value;
            }
            existing.Remove(section);
            existing[section] = contentNode?.DeepClone();
        }

        ParsedContent = existing.ToJsonString();
        ParseStatus = ResumeParseStatus.PartiallyParsed;
        AddToSectionList(ref section, isParsed: true);
    }

    public void MarkSectionFailed(string section)
    {
        AddToSectionList(ref section, isParsed: false);
    }

    public bool AreAllSectionsComplete()
    {
        var completed = (ParsedSections ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        var failed = (FailedSections ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        return RequiredSections.All(s => completed.Contains(s) || failed.Contains(s));
    }

    public void MarkFullyParsed()
    {
        ParseStatus = ResumeParseStatus.Parsed;
    }

    private void AddToSectionList(ref string section, bool isParsed)
    {
        if (isParsed)
        {
            var sections = (ParsedSections ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!sections.Contains(section))
            {
                sections.Add(section);
                ParsedSections = string.Join(",", sections);
            }
        }
        else
        {
            var sections = (FailedSections ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!sections.Contains(section))
            {
                sections.Add(section);
                FailedSections = string.Join(",", sections);
            }
        }
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
