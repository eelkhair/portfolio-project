using Shouldly;
using JobBoard.Domain.ValueObjects.Job;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class JobSalaryRangeTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var result = JobSalaryRange.Create("$80,000 - $120,000");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("$80,000 - $120,000");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = JobSalaryRange.Create("  $80k - $120k  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("$80k - $120k");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceed(string? value)
    {
        var result = JobSalaryRange.Create(value);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var longRange = new string('a', 101);

        var result = JobSalaryRange.Create(longRange);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("SalaryRange.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var range = new string('a', 100);

        var result = JobSalaryRange.Create(range);

        result.IsSuccess.ShouldBeTrue();
    }
}
