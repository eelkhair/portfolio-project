using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyStatusTests
{
    [Fact]
    public void Create_WithValidStatus_ShouldSucceed()
    {
        var result = CompanyStatus.Create("Active");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Active");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = CompanyStatus.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Status.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyStatus.Create(new string('a', 31));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Status.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyStatus.Create(new string('a', 30));

        result.IsSuccess.ShouldBeTrue();
    }
}
