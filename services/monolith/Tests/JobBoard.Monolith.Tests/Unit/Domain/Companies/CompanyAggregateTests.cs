using Shouldly;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyAggregateTests
{
    private static CompanyInput CreateValidInput(
        string name = "Acme Corp",
        string email = "info@acme.com",
        string status = "Active",
        int industryId = 1) =>
        new(
            InternalId: 1,
            Id: Guid.NewGuid(),
            Name: name,
            Email: email,
            Status: status,
            IndustryId: industryId
        );

    [Fact]
    public void Create_WithValidInput_ShouldReturnCompany()
    {
        var input = CreateValidInput();

        var company = Company.Create(input);

        company.Name.ShouldBe("Acme Corp");
        company.Email.ShouldBe("info@acme.com");
        company.Status.ShouldBe("Active");
        company.IndustryId.ShouldBe(1);
        company.InternalId.ShouldBe(input.InternalId);
        company.Id.ShouldBe(input.Id);
    }

    [Fact]
    public void Create_WithOptionalFields_ShouldSetAllProperties()
    {
        var founded = new DateTime(2020, 1, 1);
        var input = new CompanyInput(
            InternalId: 1,
            Id: Guid.NewGuid(),
            Name: "Acme Corp",
            Email: "info@acme.com",
            Status: "Active",
            IndustryId: 1,
            Description: "A great company",
            Website: "https://acme.com",
            Phone: "+1234567890",
            About: "We build things",
            Founded: founded,
            Size: "50-100"
        );

        var company = Company.Create(input);

        company.Description.ShouldBe("A great company");
        company.Website.ShouldBe("https://acme.com");
        company.Phone.ShouldBe("+1234567890");
        company.About.ShouldBe("We build things");
        company.Founded.ShouldBe(founded);
        company.Size.ShouldBe("50-100");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        var input = CreateValidInput(name: "");

        var act = () => Company.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Name.Empty");
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrowDomainException()
    {
        var input = CreateValidInput(email: "not-valid");

        var act = () => Company.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Email.InvalidFormat");
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ShouldAccumulateErrors()
    {
        var input = CreateValidInput(name: "", email: "", status: "");

        var act = () => Company.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Create_WithInvalidIndustryId_ShouldThrowDomainException()
    {
        var input = CreateValidInput(industryId: 0);

        var act = () => Company.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Company.InvalidIndustryId");
    }

    [Fact]
    public void AddJob_WithNull_ShouldThrowDomainException()
    {
        var company = Company.Create(CreateValidInput());

        var act = () => company.AddJob(null!);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldHaveSingleItem().Code.ShouldBe("Company.NullJob");
    }

    [Fact]
    public void SetName_WithValidValue_ShouldUpdateName()
    {
        var company = Company.Create(CreateValidInput());

        company.SetName("New Name");

        company.Name.ShouldBe("New Name");
    }

    [Fact]
    public void SetName_WithInvalidValue_ShouldThrowDomainException()
    {
        var company = Company.Create(CreateValidInput());

        var act = () => company.SetName("");

        Should.Throw<DomainException>(act);
    }
}
