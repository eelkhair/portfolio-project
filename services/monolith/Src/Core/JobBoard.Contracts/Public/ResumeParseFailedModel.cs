namespace JobBoard.Monolith.Contracts.Public;

public class ResumeParseFailedModel
{
    public Guid ResumeUId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}
