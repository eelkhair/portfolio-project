using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyNameTests
{
    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        var result = CompanyName.Create("Acme Corp");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Acme Corp");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = CompanyName.Create("  Acme Corp  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Acme Corp");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = CompanyName.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Name.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var longName = new string('a', 251);

        var result = CompanyName.Create(longName);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Name.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var name = new string('a', 250);

        var result = CompanyName.Create(name);

        result.IsSuccess.ShouldBeTrue();
    }
}
