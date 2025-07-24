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
    public ICollection<Responsibility> Responsibilities { get; set; } = [];
    public ICollection<Qualification> Qualifications { get; set; } = [];
    public Company? Company { get; set; }
}