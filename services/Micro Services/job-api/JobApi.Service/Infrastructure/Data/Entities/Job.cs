using Elkhair.Dev.Common.Domain.Entities;
using JobAPI.Contracts.Enums;

namespace JobApi.Infrastructure.Data.Entities;

public class Job : BaseAuditableEntity
{
    public required string Title { get; set; }
    public int CompanyId { get; set; }
    public required string Location { get; set; }
    public required JobType JobType { get; set; }
    public required string AboutRole { get; set; }
    public string? SalaryRange { get; set; }
    public ICollection<Responsibility> Responsibilities { get; set; } = [];
    public ICollection<Qualification> Qualifications { get; set; } = [];
    public Company Company { get; set; } = null!;
}