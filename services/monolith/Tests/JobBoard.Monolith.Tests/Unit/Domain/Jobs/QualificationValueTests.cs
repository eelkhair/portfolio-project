using Shouldly;
using JobBoard.Domain.ValueObjects.Qualification;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class QualificationValueTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var result = QualificationValue.Create("5+ years of experience in C#");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("5+ years of experience in C#");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = QualificationValue.Create("  Strong communication skills  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Strong communication skills");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = QualificationValue.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Value.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = QualificationValue.Create(new string('a', 251));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Value.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = QualificationValue.Create(new string('a', 250));

        result.IsSuccess.ShouldBeTrue();
    }
}
