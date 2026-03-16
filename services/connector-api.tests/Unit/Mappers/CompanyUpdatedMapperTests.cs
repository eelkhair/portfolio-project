using ConnectorAPI.Mappers;
using ConnectorAPI.Models.CompanyUpdated;
using JobBoard.IntegrationEvents.Company;
using Shouldly;

namespace connector_api.tests.Unit.Mappers;

[Trait("Category", "Unit")]
public class CompanyUpdatedMapperTests
{
    private readonly CompanyUpdatedV1Event _event = new(
        CompanyUId: Guid.NewGuid(),
        IndustryUId: Guid.NewGuid())
    {
        UserId = "user-upd-1"
    };

    [Fact]
    public void Map_MapsCompanyPayloadCorrectly()
    {
        var company = new CompanyUpdateCompanyResult
        {
            Name = "Acme Updated",
            Email = "contact@acme.com",
            Website = "https://acme.com",
            Phone = "+1-555-0100",
            Description = "Leading tech firm",
            About = "We build software",
            EEO = "Equal opportunity employer",
            Founded = new DateTime(2015, 6, 15),
            Size = "100-500",
            Logo = "https://acme.com/logo.png",
            IndustryUId = Guid.NewGuid()
        };

        var result = CompanyUpdatedMapper.Map(_event, company);

        result.Company.Name.ShouldBe(company.Name);
        result.Company.CompanyEmail.ShouldBe(company.Email);
        result.Company.CompanyWebsite.ShouldBe(company.Website);
        result.Company.Phone.ShouldBe(company.Phone);
        result.Company.Description.ShouldBe(company.Description);
        result.Company.About.ShouldBe(company.About);
        result.Company.EEO.ShouldBe(company.EEO);
        result.Company.Founded.ShouldBe(company.Founded);
        result.Company.Size.ShouldBe(company.Size);
        result.Company.Logo.ShouldBe(company.Logo);
        result.Company.IndustryUId.ShouldBe(company.IndustryUId);
    }

    [Fact]
    public void Map_MapsJobPayloadWithNameOnly()
    {
        var company = new CompanyUpdateCompanyResult
        {
            Name = "JobName Corp",
            Email = "e@e.com",
            IndustryUId = Guid.NewGuid()
        };

        var result = CompanyUpdatedMapper.Map(_event, company);

        result.Job.Name.ShouldBe(company.Name);
    }

    [Fact]
    public void Map_NullOptionalFields_MapsNulls()
    {
        var company = new CompanyUpdateCompanyResult
        {
            Name = "Minimal Corp",
            Email = "min@corp.com",
            Website = null,
            Phone = null,
            Description = null,
            About = null,
            EEO = null,
            Founded = null,
            Size = null,
            Logo = null,
            IndustryUId = Guid.NewGuid()
        };

        var result = CompanyUpdatedMapper.Map(_event, company);

        result.Company.CompanyWebsite.ShouldBeNull();
        result.Company.Phone.ShouldBeNull();
        result.Company.Description.ShouldBeNull();
        result.Company.About.ShouldBeNull();
        result.Company.EEO.ShouldBeNull();
        result.Company.Founded.ShouldBeNull();
        result.Company.Size.ShouldBeNull();
        result.Company.Logo.ShouldBeNull();
    }
}
