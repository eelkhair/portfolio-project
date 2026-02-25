using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyPhoneTests
{
    [Fact]
    public void Create_WithValidPhone_ShouldSucceed()
    {
        var result = CompanyPhone.Create("+1234567890");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("+1234567890");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceed(string? value)
    {
        var result = CompanyPhone.Create(value);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyPhone.Create(new string('1', 31));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Phone.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyPhone.Create(new string('1', 30));

        result.IsSuccess.ShouldBeTrue();
    }
}
