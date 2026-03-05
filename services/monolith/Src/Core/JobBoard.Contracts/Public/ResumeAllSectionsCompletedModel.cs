namespace JobBoard.Monolith.Contracts.Public;

public class ResumeAllSectionsCompletedModel
{
    public Guid ResumeUId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}
