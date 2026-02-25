using Shouldly;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class ResponsibilityTests
{
    [Fact]
    public void Create_WithValidValue_ShouldReturnResponsibility()
    {
        var responsibility = Responsibility.Create("Design and implement APIs");

        responsibility.Value.ShouldBe("Design and implement APIs");
    }

    [Fact]
    public void Create_WithEmptyValue_ShouldThrowDomainException()
    {
        var act = () => Responsibility.Create("");

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Value.Empty");
    }

    [Fact]
    public void Create_WithAuditInfo_ShouldApplyAudit()
    {
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        var responsibility = Responsibility.Create("Code review", createdAt, "admin@test.com");

        responsibility.CreatedAt.ShouldBe(createdAt);
        responsibility.CreatedBy.ShouldBe("admin@test.com");
    }

    [Fact]
    public void SetValue_WithValidValue_ShouldUpdate()
    {
        var responsibility = Responsibility.Create("Old value");

        responsibility.SetValue("New value");

        responsibility.Value.ShouldBe("New value");
    }

    [Fact]
    public void SetValue_WithInvalidValue_ShouldThrowDomainException()
    {
        var responsibility = Responsibility.Create("Valid");

        var act = () => responsibility.SetValue("");

        Should.Throw<DomainException>(act);
    }
}
