using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanySizeTests
{
    [Fact]
    public void Create_WithValidSize_ShouldSucceed()
    {
        var result = CompanySize.Create("50-100");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("50-100");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceedWithNull(string? value)
    {
        var result = CompanySize.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBeNull();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanySize.Create(new string('a', 31));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("CompanySize.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanySize.Create(new string('a', 30));

        result.IsSuccess.ShouldBeTrue();
    }
}
