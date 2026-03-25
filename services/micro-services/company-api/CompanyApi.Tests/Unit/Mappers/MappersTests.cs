using CompanyApi.Infrastructure.Data.Entities;
using CompanyApi.Tests.Helpers;
using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Mapster;
using Shouldly;

namespace CompanyApi.Tests.Unit.Mappers;

[Trait("Category", "Unit")]
public class MappersTests
{
    public MappersTests()
    {
        MapsterSetup.Initialize();
    }

    // ── CreateCompanyRequest → Company ──

    [Fact]
    public void CreateCompanyRequest_ToCompany_ShouldMapEmailFromCompanyEmail()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            CompanyEmail = "info@acme.com",
            CompanyWebsite = "https://acme.com",
            IndustryUId = Guid.NewGuid()
        };

        var company = request.Adapt<Company>();

        company.Email.ShouldBe("info@acme.com");
    }

    [Fact]
    public void CreateCompanyRequest_ToCompany_ShouldMapWebsiteFromCompanyWebsite()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            CompanyEmail = "info@acme.com",
            CompanyWebsite = "https://acme.com",
            IndustryUId = Guid.NewGuid()
        };

        var company = request.Adapt<Company>();

        company.Website.ShouldBe("https://acme.com");
    }

    [Fact]
    public void CreateCompanyRequest_ToCompany_ShouldMapName()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            CompanyEmail = "info@acme.com",
            IndustryUId = Guid.NewGuid()
        };

        var company = request.Adapt<Company>();

        company.Name.ShouldBe("Acme Corp");
    }

    [Fact]
    public void CreateCompanyRequest_ToCompany_ShouldSetCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;

        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            CompanyEmail = "info@acme.com",
            IndustryUId = Guid.NewGuid()
        };

        var company = request.Adapt<Company>();

        var after = DateTime.UtcNow;
        company.CreatedAt.ShouldBeInRange(before, after);
    }

    [Fact]
    public void CreateCompanyRequest_ToCompany_ShouldSetUpdatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;

        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            CompanyEmail = "info@acme.com",
            IndustryUId = Guid.NewGuid()
        };

        var company = request.Adapt<Company>();

        company.UpdatedAt.ShouldNotBeNull();
        company.UpdatedAt.Value.ShouldBeInRange(before, DateTime.UtcNow);
    }

    [Fact]
    public void CreateCompanyRequest_ToCompany_ShouldHandleNullWebsite()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corp",
            CompanyEmail = "info@acme.com",
            CompanyWebsite = null,
            IndustryUId = Guid.NewGuid()
        };

        var company = request.Adapt<Company>();

        company.Website.ShouldBeNull();
    }

    // ── Company → CompanyResponse ──

    [Fact]
    public void Company_ToCompanyResponse_ShouldMapIndustryUIdFromIndustry()
    {
        var industryUId = Guid.NewGuid();
        var company = new Company
        {
            Name = "Acme Corp",
            Email = "info@acme.com",
            Status = "Active",
            IndustryId = 1,
            Industry = new Industry { UId = industryUId, Name = "Tech" }
        };

        var response = company.Adapt<CompanyResponse>();

        response.IndustryUId.ShouldBe(industryUId);
    }

    [Fact]
    public void Company_ToCompanyResponse_ShouldMapAllScalarFields()
    {
        var company = new Company
        {
            UId = Guid.NewGuid(),
            Name = "Acme Corp",
            Email = "info@acme.com",
            Website = "https://acme.com",
            Phone = "555-1234",
            Description = "A great company",
            About = "About us",
            EEO = "EEO statement",
            Founded = new DateTime(2020, 1, 1),
            Size = "50-100",
            Logo = "logo.png",
            Status = "Active",
            IndustryId = 1,
            Industry = new Industry { UId = Guid.NewGuid(), Name = "Tech" }
        };

        var response = company.Adapt<CompanyResponse>();

        response.Name.ShouldBe("Acme Corp");
        response.Email.ShouldBe("info@acme.com");
        response.Website.ShouldBe("https://acme.com");
        response.Phone.ShouldBe("555-1234");
        response.Description.ShouldBe("A great company");
        response.About.ShouldBe("About us");
        response.EEO.ShouldBe("EEO statement");
        response.Founded.ShouldBe(new DateTime(2020, 1, 1));
        response.Size.ShouldBe("50-100");
        response.Logo.ShouldBe("logo.png");
        response.Status.ShouldBe("Active");
    }

    // ── Industry → IndustryResponse ──

    [Fact]
    public void Industry_ToIndustryResponse_ShouldMapAllFields()
    {
        var uid = Guid.NewGuid();
        var industry = new Industry
        {
            UId = uid,
            Name = "Technology",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var response = industry.Adapt<IndustryResponse>();

        response.UId.ShouldBe(uid);
        response.Name.ShouldBe("Technology");
        response.CreatedAt.ShouldBe(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        response.UpdatedAt.ShouldBe(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Industry_ToIndustryResponse_ShouldHandleNullUpdatedAt()
    {
        var industry = new Industry
        {
            UId = Guid.NewGuid(),
            Name = "Finance",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        var response = industry.Adapt<IndustryResponse>();

        response.UpdatedAt.ShouldBeNull();
    }
}
