using JobBoard.IntegrationEvents.Resume;

namespace JobBoard.Monolith.Contracts.Public;

public class ResumeParseCompletedModel
{
    public Guid ResumeUId { get; set; }
    public ResumeParsedContentResponse ParsedContent { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}
