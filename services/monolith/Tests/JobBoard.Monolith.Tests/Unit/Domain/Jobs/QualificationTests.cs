using Shouldly;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class QualificationTests
{
    [Fact]
    public void Create_WithValidValue_ShouldReturnQualification()
    {
        var qualification = Qualification.Create("5+ years C# experience");

        qualification.Value.ShouldBe("5+ years C# experience");
    }

    [Fact]
    public void Create_WithEmptyValue_ShouldThrowDomainException()
    {
        var act = () => Qualification.Create("");

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Value.Empty");
    }

    [Fact]
    public void Create_WithAuditInfo_ShouldApplyAudit()
    {
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        var qualification = Qualification.Create("5+ years C#", createdAt, "admin@test.com");

        qualification.CreatedAt.ShouldBe(createdAt);
        qualification.CreatedBy.ShouldBe("admin@test.com");
    }

    [Fact]
    public void SetValue_WithValidValue_ShouldUpdate()
    {
        var qualification = Qualification.Create("Old value");

        qualification.SetValue("New value");

        qualification.Value.ShouldBe("New value");
    }

    [Fact]
    public void SetValue_WithInvalidValue_ShouldThrowDomainException()
    {
        var qualification = Qualification.Create("Valid");

        var act = () => qualification.SetValue("");

        Should.Throw<DomainException>(act);
    }
}
