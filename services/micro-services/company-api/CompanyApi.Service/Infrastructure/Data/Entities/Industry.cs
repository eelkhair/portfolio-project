using Elkhair.Dev.Common.Domain.Entities;

namespace CompanyApi.Infrastructure.Data.Entities;

public class Industry : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    
    public ICollection<Company> Companies { get; set; } = new List<Company>();
}