using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyEEOTests
{
    [Fact]
    public void Create_WithValidEEO_ShouldSucceed()
    {
        var result = CompanyEEO.Create("Equal opportunity employer");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Equal opportunity employer");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceed(string? value)
    {
        var result = CompanyEEO.Create(value);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyEEO.Create(new string('a', 501));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("EEO.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyEEO.Create(new string('a', 500));

        result.IsSuccess.ShouldBeTrue();
    }
}
