using Shouldly;
using JobBoard.Domain.ValueObjects.Job;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class JobTitleTests
{
    [Fact]
    public void Create_WithValidTitle_ShouldSucceed()
    {
        var result = JobTitle.Create("Senior Software Engineer");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Senior Software Engineer");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = JobTitle.Create("  Software Engineer  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Software Engineer");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = JobTitle.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Title.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = JobTitle.Create(new string('a', 251));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Title.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = JobTitle.Create(new string('a', 250));

        result.IsSuccess.ShouldBeTrue();
    }
}
