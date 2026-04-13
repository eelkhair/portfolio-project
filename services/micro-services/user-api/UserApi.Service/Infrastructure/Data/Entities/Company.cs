using Elkhair.Dev.Common.Domain.Entities;

namespace UserApi.Infrastructure.Data.Entities;

public class Company : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string KeycloakGroupId { get; set; } = null!;

    public ICollection<UserCompany>? UserCompanies { get; set; }
}
