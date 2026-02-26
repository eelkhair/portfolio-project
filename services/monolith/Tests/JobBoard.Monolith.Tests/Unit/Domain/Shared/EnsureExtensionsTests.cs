using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.Company;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Domain.Shared;

[Trait("Category", "Unit")]
public class EnsureExtensionsTests
{
    [Fact]
    public void Ensure_WhenSuccess_ShouldReturnValue()
    {
        var result = CompanyName.Create("ValidName");

        var value = result.Ensure<CompanyName, string>("Company.InvalidName");

        value.ShouldBe("ValidName");
    }

    [Fact]
    public void Ensure_WhenFailure_ShouldThrowDomainException()
    {
        var result = CompanyName.Create("");

        var ex = Should.Throw<DomainException>(
            () => result.Ensure<CompanyName, string>("Company.InvalidName"));

        ex.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Ensure_WhenFailure_ShouldContainCorrectErrorCode()
    {
        var result = CompanyName.Create("");

        var ex = Should.Throw<DomainException>(
            () => result.Ensure<CompanyName, string>("Company.InvalidName"));

        ex.Message.ShouldContain("Company.InvalidName");
    }

    [Fact]
    public void Ensure_WhenFailure_ShouldPreserveValidationErrors()
    {
        var result = CompanyName.Create("");

        var ex = Should.Throw<DomainException>(
            () => result.Ensure<CompanyName, string>("Company.InvalidName"));

        ex.Errors.ShouldContain(e => e.Code == "Name.Empty");
    }
}
