using Elkhair.Dev.Common.Domain.Entities;

namespace JobApi.Data.Entities;

public class Job : BaseAuditableEntity
{
    public required string Title { get; set; }
    public int CompanyId { get; set; }
    public required string Location { get; set; }
    public required string JobType { get; set; }
    public required string AboutRole { get; set; }
    public string? SalaryRange { get; set; }
    public string? PostedAt { get; set; }
    public string? AboutCompany { get; set; }
    public string? EEO { get; set; }  
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
    public Company? Company { get; set; }
}