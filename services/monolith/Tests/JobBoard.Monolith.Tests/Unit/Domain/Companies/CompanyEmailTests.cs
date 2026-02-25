using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyEmailTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldSucceed()
    {
        var result = CompanyEmail.Create("info@acme.com");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("info@acme.com");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = CompanyEmail.Create("  info@acme.com  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("info@acme.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = CompanyEmail.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Email.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var longEmail = new string('a', 90) + "@acme.commmm";

        var result = CompanyEmail.Create(longEmail);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == "Email.TooLong");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@no-local.com")]
    public void Create_WithInvalidFormat_ShouldFail(string value)
    {
        var result = CompanyEmail.Create(value);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == "Email.InvalidFormat");
    }
}
