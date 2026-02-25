using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyWebsiteTests
{
    [Fact]
    public void Create_WithValidWebsite_ShouldSucceed()
    {
        var result = CompanyWebsite.Create("https://acme.com");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("https://acme.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceed(string? value)
    {
        var result = CompanyWebsite.Create(value);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyWebsite.Create(new string('a', 201));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Website.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyWebsite.Create(new string('a', 200));

        result.IsSuccess.ShouldBeTrue();
    }
}
