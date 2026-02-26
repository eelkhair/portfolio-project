namespace JobBoard.Monolith.Contracts.Jobs;

public class JobResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Guid CompanyUId { get; set; }
    public string CompanyName { get; set; }
    public string Location { get; set; }
    public JobType JobType { get; set; }
    public string AboutRole { get; set; }
    public string? SalaryRange { get; set; }
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
};