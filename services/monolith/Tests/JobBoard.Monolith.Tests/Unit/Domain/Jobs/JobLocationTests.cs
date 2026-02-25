using Shouldly;
using JobBoard.Domain.ValueObjects.Job;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class JobLocationTests
{
    [Fact]
    public void Create_WithValidLocation_ShouldSucceed()
    {
        var result = JobLocation.Create("New York, NY");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("New York, NY");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = JobLocation.Create("  Remote  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Remote");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = JobLocation.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Location.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = JobLocation.Create(new string('a', 151));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Location.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = JobLocation.Create(new string('a', 150));

        result.IsSuccess.ShouldBeTrue();
    }
}
