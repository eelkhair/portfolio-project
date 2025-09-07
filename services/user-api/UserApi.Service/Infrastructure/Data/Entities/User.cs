using Elkhair.Dev.Common.Domain.Entities;

namespace UserApi.Infrastructure.Data.Entities;

public class User : BaseAuditableEntity
{
    public required string Email { get; set; }
}