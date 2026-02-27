namespace JobBoard.AI.Application.Actions.Jobs;

public class PublishedJobEvent
{
    public Guid UId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid CompanyUId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int JobType { get; set; }
    public string AboutRole { get; set; } = string.Empty;
    public string? SalaryRange { get; set; }
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? DraftId { get; set; }
    public bool DeleteDraft { get; set; }
}