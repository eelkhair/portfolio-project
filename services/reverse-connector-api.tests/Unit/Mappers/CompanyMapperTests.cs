using JobBoard.IntegrationEvents.Company;
using ReverseConnectorAPI.Mappers;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Mappers;

public class CompanyMapperTests
{
    [Fact]
    public void ToCreatePayload_MapsAllFields()
    {
        var companyId = Guid.NewGuid();
        var industryId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userCompanyId = Guid.NewGuid();

        var evt = new MicroCompanyCreatedV1Event(
            CompanyUId: companyId,
            Name: "TestCorp",
            CompanyEmail: "hr@testcorp.com",
            CompanyWebsite: "https://testcorp.com",
            IndustryUId: industryId,
            AdminFirstName: "John",
            AdminLastName: "Doe",
            AdminEmail: "john@testcorp.com",
            AdminUId: adminId,
            UserCompanyUId: userCompanyId
        );

        var result = CompanyMapper.ToCreatePayload(evt);

        result.CompanyId.ShouldBe(companyId);
        result.Name.ShouldBe("TestCorp");
        result.CompanyEmail.ShouldBe("hr@testcorp.com");
        result.CompanyWebsite.ShouldBe("https://testcorp.com");
        result.IndustryUId.ShouldBe(industryId);
        result.AdminFirstName.ShouldBe("John");
        result.AdminLastName.ShouldBe("Doe");
        result.AdminEmail.ShouldBe("john@testcorp.com");
        result.AdminUId.ShouldBe(adminId);
        result.UserCompanyUId.ShouldBe(userCompanyId);
    }

    [Fact]
    public void ToCreatePayload_WithNullOptionalFields_MapsNulls()
    {
        var evt = new MicroCompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            Name: "MinimalCorp",
            CompanyEmail: "info@minimal.com",
            CompanyWebsite: null,
            IndustryUId: Guid.NewGuid(),
            AdminFirstName: "Jane",
            AdminLastName: "Smith",
            AdminEmail: "jane@minimal.com",
            AdminUId: null,
            UserCompanyUId: null
        );

        var result = CompanyMapper.ToCreatePayload(evt);

        result.CompanyWebsite.ShouldBeNull();
        result.AdminUId.ShouldBeNull();
        result.UserCompanyUId.ShouldBeNull();
    }

    [Fact]
    public void ToUpdatePayload_MapsAllFields()
    {
        var companyId = Guid.NewGuid();
        var industryId = Guid.NewGuid();
        var founded = new DateTime(2020, 1, 15);

        var evt = new MicroCompanyUpdatedV1Event(
            CompanyUId: companyId,
            Name: "UpdatedCorp",
            CompanyEmail: "updated@corp.com",
            CompanyWebsite: "https://updated.com",
            Phone: "+1234567890",
            Description: "A great company",
            About: "We build things",
            EEO: "Equal opportunity employer",
            Founded: founded,
            Size: "50-200",
            Logo: "https://cdn.test/logo.png",
            IndustryUId: industryId
        );

        var result = CompanyMapper.ToUpdatePayload(evt);

        result.CompanyId.ShouldBe(companyId);
        result.Name.ShouldBe("UpdatedCorp");
        result.CompanyEmail.ShouldBe("updated@corp.com");
        result.CompanyWebsite.ShouldBe("https://updated.com");
        result.Phone.ShouldBe("+1234567890");
        result.Description.ShouldBe("A great company");
        result.About.ShouldBe("We build things");
        result.EEO.ShouldBe("Equal opportunity employer");
        result.Founded.ShouldBe(founded);
        result.Size.ShouldBe("50-200");
        result.Logo.ShouldBe("https://cdn.test/logo.png");
        result.IndustryUId.ShouldBe(industryId);
    }

    [Fact]
    public void ToUpdatePayload_WithNullOptionalFields_MapsNulls()
    {
        var evt = new MicroCompanyUpdatedV1Event(
            CompanyUId: Guid.NewGuid(),
            Name: "MinimalUpdate",
            CompanyEmail: "min@test.com",
            CompanyWebsite: null,
            Phone: null,
            Description: null,
            About: null,
            EEO: null,
            Founded: null,
            Size: null,
            Logo: null,
            IndustryUId: Guid.NewGuid()
        );

        var result = CompanyMapper.ToUpdatePayload(evt);

        result.CompanyWebsite.ShouldBeNull();
        result.Phone.ShouldBeNull();
        result.Description.ShouldBeNull();
        result.About.ShouldBeNull();
        result.EEO.ShouldBeNull();
        result.Founded.ShouldBeNull();
        result.Size.ShouldBeNull();
        result.Logo.ShouldBeNull();
    }
}
