namespace JobBoard.Monolith.Contracts.Jobs;

public class JobBatchResponse
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AboutRole { get; set; }
    public string? Location { get; set; }
    public string? JobType { get; set; }
    public string? SalaryRange { get; set; }
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
}
