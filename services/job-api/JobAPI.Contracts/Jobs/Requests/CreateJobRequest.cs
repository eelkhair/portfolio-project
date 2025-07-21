namespace JobAPI.Contracts.Jobs.Requests;

public class CreateJobRequest 
{
    public required string Title { get; set; }
    public required string Company { get; set; }
    public required string Location { get; set; }
    public required string JobType { get; set; }
    public required string AboutRole { get; set; }
    public string? SalaryRange { get; set; }
    public string? PostedAt { get; set; }
    public string? AboutCompany { get; set; }
    public string? EEO { get; set; }  
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
}