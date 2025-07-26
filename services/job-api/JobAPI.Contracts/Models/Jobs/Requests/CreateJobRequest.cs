using JobAPI.Contracts.Enums;

namespace JobAPI.Contracts.Models.Jobs.Requests;

public class CreateJobRequest 
{
    public required string Title { get; set; }
    public Guid CompanyUId { get; set; }
    public required string Location { get; set; }
    public required JobType JobType { get; set; }
    public required string AboutRole { get; set; }
    public string? SalaryRange { get; set; }
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
}