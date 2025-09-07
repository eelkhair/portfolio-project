using Elkhair.Dev.Common.Domain.Entities;

namespace JobApi.Infrastructure.Data.Entities;

public class Company : BaseAuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Industry { get; set; }
    public string? Website { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public string? About { get; set; }
    public string? EEO { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }
    public required bool IsActive { get; set; }
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}