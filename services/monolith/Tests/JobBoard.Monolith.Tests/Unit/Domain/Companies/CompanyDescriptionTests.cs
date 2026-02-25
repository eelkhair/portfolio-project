using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyDescriptionTests
{
    [Fact]
    public void Create_WithValidDescription_ShouldSucceed()
    {
        var result = CompanyDescription.Create("A technology company");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("A technology company");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceedWithNull(string? value)
    {
        var result = CompanyDescription.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBeNull();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyDescription.Create(new string('a', 4001));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Description.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyDescription.Create(new string('a', 4000));

        result.IsSuccess.ShouldBeTrue();
    }
}
