using Elkhair.Dev.Common.Domain.Entities;

namespace UserApi.Infrastructure.Data.Entities;

public class UserCompany: BaseAuditableEntity
{
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public Company Company { get; set; } = null!;
    public User User { get; set; } = null!;
    
}