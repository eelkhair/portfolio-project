using JobBoard.Domain.Enums;

namespace JobBoard.Domain.Aggregates;

public class JobInput
{
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AboutRole { get; set; } = string.Empty;
    public string? SalaryRange { get; set; }
    public JobType JobType { get; set; }
    public int CompanyId { get; set; }

    public List<string>? Responsibilities { get; set; }
    public List<string>? Qualifications { get; set; }

    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}