using Shouldly;
using JobBoard.Domain.ValueObjects.Job;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class JobAboutRoleTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var result = JobAboutRole.Create("We are looking for a talented engineer");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("We are looking for a talented engineer");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = JobAboutRole.Create("  About this role  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("About this role");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = JobAboutRole.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("AboutRole.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = JobAboutRole.Create(new string('a', 3001));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("AboutRole.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = JobAboutRole.Create(new string('a', 3000));

        result.IsSuccess.ShouldBeTrue();
    }
}
