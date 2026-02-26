using ConnectorAPI.Mappers;
using ConnectorAPI.Models.CompanyCreated;
using JobBoard.IntegrationEvents.Company;
using Shouldly;

namespace connector_api.tests.Unit.Mappers;

[Trait("Category", "Unit")]
public class CompanyCreatedMapperTests
{
    private readonly CompanyCreatedV1Event _event = new(
        CompanyUId: Guid.NewGuid(),
        IndustryUId: Guid.NewGuid(),
        AdminUId: Guid.NewGuid(),
        UserCompanyUId: Guid.NewGuid())
    {
        UserId = "auth0|user-123"
    };

    private readonly CompanyCreateCompanyResult _company = new()
    {
        Name = "Acme Corp",
        Email = "info@acme.com",
        Website = "https://acme.com",
        IndustryId = Guid.NewGuid()
    };

    private readonly CompanyCreateUserResult _admin = new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@acme.com"
    };

    [Fact]
    public void Map_CompanyPayload_ShouldMapCompanyId()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Company.CompanyId.ShouldBe(_event.CompanyUId);
    }

    [Fact]
    public void Map_CompanyPayload_ShouldMapNameAndEmail()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Company.Name.ShouldBe(_company.Name);
        result.Company.CompanyEmail.ShouldBe(_company.Email);
    }

    [Fact]
    public void Map_CompanyPayload_ShouldMapWebsite()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Company.CompanyWebsite.ShouldBe(_company.Website);
    }

    [Fact]
    public void Map_CompanyPayload_WithNullWebsite_ShouldBeNull()
    {
        var company = new CompanyCreateCompanyResult
        {
            Name = "Test", Email = "test@test.com", Website = null, IndustryId = Guid.NewGuid()
        };

        var result = CompanyCreatedMapper.Map(_event, company, _admin);

        result.Company.CompanyWebsite.ShouldBeNull();
    }

    [Fact]
    public void Map_CompanyPayload_ShouldMapIndustryAndUserId()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Company.IndustryUId.ShouldBe(_company.IndustryId);
        result.Company.UserId.ShouldBe(_event.UserId);
    }

    [Fact]
    public void Map_JobPayload_ShouldMapUIdAndName()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Job.Data.UId.ShouldBe(_event.CompanyUId);
        result.Job.Data.Name.ShouldBe(_company.Name);
    }

    [Fact]
    public void Map_JobPayload_ShouldSetUserIdFromEvent()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Job.UserId.ShouldBe(_event.UserId);
    }

    [Fact]
    public void Map_JobPayload_ShouldHaveValidIdempotencyKey()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Job.IdempotencyKey.ShouldNotBeNullOrEmpty();
        Guid.TryParse(result.Job.IdempotencyKey, out _).ShouldBeTrue();
    }

    [Fact]
    public void Map_UserPayload_ShouldMapAdminFields()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.User.Data.UId.ShouldBe(_event.AdminUId);
        result.User.Data.FirstName.ShouldBe(_admin.FirstName);
        result.User.Data.LastName.ShouldBe(_admin.LastName);
        result.User.Data.Email.ShouldBe(_admin.Email);
    }

    [Fact]
    public void Map_UserPayload_ShouldMapCompanyFields()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.User.Data.CompanyName.ShouldBe(_company.Name);
        result.User.Data.CompanyUId.ShouldBe(_event.CompanyUId);
        result.User.Data.UserCompanyUId.ShouldBe(_event.UserCompanyUId);
    }

    [Fact]
    public void Map_UserPayload_ShouldSetUserIdFromEvent()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.User.UserId.ShouldBe(_event.UserId);
    }

    [Fact]
    public void Map_UserPayload_ShouldHaveValidIdempotencyKey()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.User.IdempotencyKey.ShouldNotBeNullOrEmpty();
        Guid.TryParse(result.User.IdempotencyKey, out _).ShouldBeTrue();
    }

    [Fact]
    public void Map_IdempotencyKeys_ShouldBeDifferent()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Job.IdempotencyKey.ShouldNotBe(result.User.IdempotencyKey);
    }

    [Fact]
    public void Map_ShouldReturnAllThreePayloads()
    {
        var result = CompanyCreatedMapper.Map(_event, _company, _admin);

        result.Company.ShouldNotBeNull();
        result.Job.ShouldNotBeNull();
        result.User.ShouldNotBeNull();
    }
}
