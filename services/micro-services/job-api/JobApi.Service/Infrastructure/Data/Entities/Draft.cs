using Elkhair.Dev.Common.Domain.Entities;

namespace JobApi.Infrastructure.Data.Entities;

public class Draft : BaseAuditableEntity
{
    public int CompanyId { get; set; }
    public required string DraftType { get; set; }
    public required string DraftStatus { get; set; }
    public required string ContentJson { get; set; }
    public Company Company { get; set; } = null!;
}
