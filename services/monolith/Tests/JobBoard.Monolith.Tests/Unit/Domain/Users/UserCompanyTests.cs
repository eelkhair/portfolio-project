using Shouldly;
using JobBoard.Domain.Entities;

namespace JobBoard.Monolith.Tests.Unit.Domain.Users;

[Trait("Category", "Unit")]
public class UserCompanyTests
{
    [Fact]
    public void Create_WithValidInput_ShouldReturnUserCompany()
    {
        var id = Guid.NewGuid();

        var link = UserCompany.Create(1, 2, 10, id);

        link.UserId.ShouldBe(1);
        link.CompanyId.ShouldBe(2);
        link.InternalId.ShouldBe(10);
        link.Id.ShouldBe(id);
    }

    [Fact]
    public void Create_WithAuditInfo_ShouldApplyAudit()
    {
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        var link = UserCompany.Create(1, 2, 10, Guid.NewGuid(), createdAt, "admin@test.com");

        link.CreatedAt.ShouldBe(createdAt);
        link.CreatedBy.ShouldBe("admin@test.com");
        link.UpdatedAt.ShouldBe(createdAt);
        link.UpdatedBy.ShouldBe("admin@test.com");
    }

    [Fact]
    public void Create_WithoutAuditInfo_ShouldHaveDefaults()
    {
        var link = UserCompany.Create(1, 2, 10, Guid.NewGuid());

        link.CreatedAt.ShouldBe(default);
        link.CreatedBy.ShouldBe(string.Empty);
    }

    [Fact]
    public void Create_WithNullCreatedBy_ShouldNotSetAuditUser()
    {
        var createdAt = new DateTime(2024, 1, 1);

        var link = UserCompany.Create(1, 2, 10, Guid.NewGuid(), createdAt, null);

        link.CreatedAt.ShouldBe(createdAt);
        link.CreatedBy.ShouldBe(string.Empty);
    }
}
