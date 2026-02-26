using JobBoard.Application.Actions.Companies.Create;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Mappers;

[Trait("Category", "Unit")]
public class CreateCompanyMappersTests
{
    private static CreateCompanyCommand CreateValidCommand() => new()
    {
        Name = "Acme Corp",
        CompanyEmail = "info@acme.com",
        CompanyWebsite = "https://acme.com",
        IndustryUId = Guid.NewGuid(),
        AdminFirstName = "John",
        AdminLastName = "Doe",
        AdminEmail = "john@acme.com",
        UserId = "user-123"
    };

    [Fact]
    public void ToCompanyEntity_ShouldMapNameAndEmail()
    {
        var command = CreateValidCommand();
        var uid = Guid.NewGuid();

        var company = command.ToCompanyEntity(uid, 1, 10);

        company.Name.ShouldBe("Acme Corp");
        company.Email.ShouldBe("info@acme.com");
    }

    [Fact]
    public void ToCompanyEntity_ShouldSetStatusToProvisioning()
    {
        var command = CreateValidCommand();

        var company = command.ToCompanyEntity(Guid.NewGuid(), 1, 10);

        company.Status.ShouldBe("Provisioning");
    }

    [Fact]
    public void ToCompanyEntity_ShouldMapIdsCorrectly()
    {
        var command = CreateValidCommand();
        var uid = Guid.NewGuid();

        var company = command.ToCompanyEntity(uid, 42, 10);

        company.Id.ShouldBe(uid);
        company.InternalId.ShouldBe(42);
        company.IndustryId.ShouldBe(10);
    }

    [Fact]
    public void ToCompanyEntity_ShouldSetWebsite()
    {
        var command = CreateValidCommand();

        var company = command.ToCompanyEntity(Guid.NewGuid(), 1, 10);

        company.Website.ShouldBe("https://acme.com");
    }

    [Fact]
    public void ToCompanyEntity_WithNullWebsite_ShouldSetNullWebsite()
    {
        var command = CreateValidCommand();
        command.CompanyWebsite = null;

        var company = command.ToCompanyEntity(Guid.NewGuid(), 1, 10);

        company.Website.ShouldBeNull();
    }

    [Fact]
    public void ToUserEntity_ShouldMapNameAndEmail()
    {
        var command = CreateValidCommand();
        var uid = Guid.NewGuid();

        var user = command.ToUserEntity(uid, 5);

        user.FirstName.ShouldBe("John");
        user.LastName.ShouldBe("Doe");
        user.Email.ShouldBe("john@acme.com");
    }

    [Fact]
    public void ToUserEntity_ShouldSetIds()
    {
        var command = CreateValidCommand();
        var uid = Guid.NewGuid();

        var user = command.ToUserEntity(uid, 5);

        user.Id.ShouldBe(uid);
        user.InternalId.ShouldBe(5);
    }

    [Fact]
    public void ToUserCompanyEntity_ShouldMapIds()
    {
        var command = CreateValidCommand();
        var uid = Guid.NewGuid();

        var userCompany = command.ToUserCompanyEntity(uid, 3, 1, 2);

        userCompany.Id.ShouldBe(uid);
        userCompany.InternalId.ShouldBe(3);
        userCompany.CompanyId.ShouldBe(1);
        userCompany.UserId.ShouldBe(2);
    }

    [Fact]
    public void ToIntegrationEvent_ShouldSetEventType()
    {
        var command = CreateValidCommand();
        var companyUId = Guid.NewGuid();
        var adminUId = Guid.NewGuid();
        var userCompanyUId = Guid.NewGuid();

        var integrationEvent = command.ToIntegrationEvent(companyUId, adminUId, userCompanyUId);

        integrationEvent.EventType.ShouldBe("company.created.v1");
    }

    [Fact]
    public void ToIntegrationEvent_ShouldMapAllUIds()
    {
        var command = CreateValidCommand();
        var companyUId = Guid.NewGuid();
        var adminUId = Guid.NewGuid();
        var userCompanyUId = Guid.NewGuid();

        var integrationEvent = command.ToIntegrationEvent(companyUId, adminUId, userCompanyUId);

        integrationEvent.CompanyUId.ShouldBe(companyUId);
        integrationEvent.AdminUId.ShouldBe(adminUId);
        integrationEvent.UserCompanyUId.ShouldBe(userCompanyUId);
    }

    [Fact]
    public void ToIntegrationEvent_ShouldSetUserId()
    {
        var command = CreateValidCommand();

        var integrationEvent = command.ToIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        integrationEvent.UserId.ShouldBe("user-123");
    }

    [Fact]
    public void ToIntegrationEvent_ShouldSetIndustryUId()
    {
        var command = CreateValidCommand();
        var industryUId = command.IndustryUId;

        var integrationEvent = command.ToIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        integrationEvent.IndustryUId.ShouldBe(industryUId);
    }
}
