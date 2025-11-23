using Elkhair.Dev.Common.Domain.Entities;

namespace JobApi.Infrastructure.Data.Entities;

public class Company : BaseAuditableEntity
{
    public required string Name { get; set; }
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}