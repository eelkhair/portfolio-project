using Dapr.Client.Autogen.Grpc.v1;
using Elkhair.Dev.Common.Domain.Entities;

namespace CompanyApi.Infrastructure.Data.Entities;

public class Company : BaseAuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? About { get; set; }
    public string? EEO { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }
    public required string Status { get; set; }
    
    public required int IndustryId { get; set; }
    public Industry Industry { get; set; } = null!;
}