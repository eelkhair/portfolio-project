using Elkhair.Dev.Common.Domain.Entities;

namespace JobApi.Data.Entities;

public class Qualification : BaseAuditableEntity
{
    public string Value { get; set; } = string.Empty;
    public int JobId { get; set; }
    public Job? Job { get; set; }   
}