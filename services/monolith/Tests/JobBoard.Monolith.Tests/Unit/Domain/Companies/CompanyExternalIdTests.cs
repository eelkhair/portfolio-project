using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyExternalIdTests
{
    [Fact]
    public void Create_WithValidId_ShouldSucceed()
    {
        var result = CompanyExternalId.Create("ext-123");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("ext-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceedWithNull(string? value)
    {
        var result = CompanyExternalId.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBeNull();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = CompanyExternalId.Create(new string('a', 51));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("ExternalId.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = CompanyExternalId.Create(new string('a', 50));

        result.IsSuccess.ShouldBeTrue();
    }
}
