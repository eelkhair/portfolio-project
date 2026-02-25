using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyAboutTests
{
    [Fact]
    public void Create_WithValidAbout_ShouldSucceed()
    {
        var result = CompanyAbout.Create("We build great software");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("We build great software");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceedWithNull(string? value)
    {
        var result = CompanyAbout.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBeNull();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyAbout.Create(new string('a', 2001));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("About.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyAbout.Create(new string('a', 2000));

        result.IsSuccess.ShouldBeTrue();
    }
}
