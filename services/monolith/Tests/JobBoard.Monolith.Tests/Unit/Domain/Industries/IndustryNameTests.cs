using Shouldly;
using JobBoard.Domain.ValueObjects.Industry;

namespace JobBoard.Monolith.Tests.Unit.Domain.Industries;

[Trait("Category", "Unit")]
public class IndustryNameTests
{
    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        var result = IndustryName.Create("Technology");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Technology");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = IndustryName.Create("  Healthcare  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Healthcare");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = IndustryName.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Name.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = IndustryName.Create(new string('a', 251));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Name.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = IndustryName.Create(new string('a', 250));

        result.IsSuccess.ShouldBeTrue();
    }
}
