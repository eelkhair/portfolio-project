using Elkhair.Dev.Common.Domain.Entities;

namespace UserApi.Infrastructure.Data.Entities;

public class User : BaseAuditableEntity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Email { get; set; }
    public string? Auth0UserId { get; set; }

    public ICollection<UserCompany> UserCompanies { get; set; }
    
}