namespace JobBoard.Monolith.Contracts.Public;

public class ResumeSectionFailedModel
{
    public Guid ResumeUId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}
