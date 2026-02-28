namespace JobBoard.AI.Infrastructure.Dapr.AITools.Shared;

public class CompanyJobSummaryDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int JobCount { get; set; }
    public List<JobSummaryDto> Jobs { get; set; } = [];
}

public class JobSummaryDto
{
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string? SalaryRange { get; set; }
    public DateTime CreatedAt { get; set; }
}
