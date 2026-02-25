using Shouldly;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Monolith.Tests.Unit.Domain.Industries;

[Trait("Category", "Unit")]
public class IndustryTests
{
    [Fact]
    public void Create_WithValidName_ShouldReturnIndustry()
    {
        var industry = Industry.Create("Technology  ");

        industry.Name.ShouldBe("Technology");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        var act = () => Industry.Create("");

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Name.Empty");
    }

    [Fact]
    public void SetName_WithValidName_ShouldUpdateName()
    {
        var industry = Industry.Create("Technology");

        industry.SetName("Healthcare");

        industry.Name.ShouldBe("Healthcare");
    }

    [Fact]
    public void SetName_WithInvalidName_ShouldThrowDomainException()
    {
        var industry = Industry.Create("Technology");

        var act = () => industry.SetName("");

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void AddCompany_WithValidCompany_ShouldAddToCollection()
    {
        var industry = Industry.Create("Technology");
        var company = Company.Create(new CompanyInput(
            InternalId: 1,
            Id: Guid.NewGuid(),
            Name: "Acme Corp",
            Email: "info@acme.com",
            Status: "Active",
            IndustryId: 1
        ));

        industry.AddCompany(company);

        industry.Companies.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddCompany_WithNull_ShouldThrowDomainException()
    {
        var industry = Industry.Create("Technology");

        var act = () => industry.AddCompany(null!);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldHaveSingleItem().Code.ShouldBe("Industry.NullCompany");
    }

    [Fact]
    public void AddCompany_WithDuplicateName_ShouldThrowDomainException()
    {
        var industry = Industry.Create("Technology");
        var company1 = Company.Create(new CompanyInput(1, Guid.NewGuid(), "Acme Corp", "a@acme.com", "Active", 1));
        var company2 = Company.Create(new CompanyInput(2, Guid.NewGuid(), "Acme Corp", "b@acme.com", "Active", 1));

        industry.AddCompany(company1);
        var act = () => industry.AddCompany(company2);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldHaveSingleItem().Code.ShouldBe("Industry.DuplicateCompany");
    }

    [Fact]
    public void AddCompany_WithDuplicateNameDifferentCase_ShouldThrowDomainException()
    {
        var industry = Industry.Create("Technology");
        var company1 = Company.Create(new CompanyInput(1, Guid.NewGuid(), "Acme Corp", "a@acme.com", "Active", 1));
        var company2 = Company.Create(new CompanyInput(2, Guid.NewGuid(), "acme corp", "b@acme.com", "Active", 1));

        industry.AddCompany(company1);
        var act = () => industry.AddCompany(company2);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldHaveSingleItem().Code.ShouldBe("Industry.DuplicateCompany");
    }

    [Fact]
    public void RemoveCompany_WithNull_ShouldNotThrow()
    {
        var industry = Industry.Create("Technology");

        var act = () => industry.RemoveCompany(null!);

        Should.NotThrow(act);
    }

    [Fact]
    public void RemoveCompany_WithExistingCompany_ShouldRemoveFromCollection()
    {
        var industry = Industry.Create("Technology");
        var company = Company.Create(new CompanyInput(1, Guid.NewGuid(), "Acme Corp", "a@acme.com", "Active", 1));
        industry.AddCompany(company);

        industry.RemoveCompany(company);

        industry.Companies.ShouldBeEmpty();
    }
}
