using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyLogoTests
{
    [Fact]
    public void Create_WithValidLogo_ShouldSucceed()
    {
        var result = CompanyLogo.Create("https://cdn.acme.com/logo.png");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("https://cdn.acme.com/logo.png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceed(string? value)
    {
        var result = CompanyLogo.Create(value);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyLogo.Create(new string('a', 401));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Logo.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyLogo.Create(new string('a', 400));

        result.IsSuccess.ShouldBeTrue();
    }
}
