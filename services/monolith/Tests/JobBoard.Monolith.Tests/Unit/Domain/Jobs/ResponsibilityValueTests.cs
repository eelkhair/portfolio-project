using Shouldly;
using JobBoard.Domain.ValueObjects.Responsibility;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class ResponsibilityValueTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var result = ResponsibilityValue.Create("Design and implement APIs");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Design and implement APIs");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = ResponsibilityValue.Create("  Code review  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Code review");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = ResponsibilityValue.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Value.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = ResponsibilityValue.Create(new string('a', 251));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Value.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = ResponsibilityValue.Create(new string('a', 250));

        result.IsSuccess.ShouldBeTrue();
    }
}
