using JobBoard.IntegrationEvents.Resume;

namespace JobBoard.Monolith.Contracts.Public;

public class ResumeResponse
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool HasParsedContent { get; set; }
    public string ParseStatus { get; set; } = "Pending";
    public int ParseRetryCount { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public ResumeParsedContentResponse? ParsedContent { get; set; }
}
