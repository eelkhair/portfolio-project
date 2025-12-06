using JobBoard.Domain.Entities.Users;

namespace JobBoard.Domain.Entities;

public class UserCompany : BaseAuditableEntity
{

    protected UserCompany() { }

    private UserCompany(int userId, int companyId)
    {
        UserId = userId;
        CompanyId = companyId;
    }

    public int UserId { get; private set; }
    public int CompanyId { get; private set; }

    public User User { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public static UserCompany Create(int userId, int companyId, int internalId, Guid id, DateTime? createdAt = null, string? createdBy = null)
    {
        var link = new UserCompany(userId, companyId)
        {
            InternalId = internalId,
            Id = id
        };
        if (createdAt.HasValue)
        {
            link.CreatedAt = createdAt.Value;
            link.UpdatedAt = createdAt.Value;
        }

        if (string.IsNullOrWhiteSpace(createdBy)) return link;
        link.CreatedBy = createdBy;
        link.UpdatedBy = createdBy;

        return link;
    }
}