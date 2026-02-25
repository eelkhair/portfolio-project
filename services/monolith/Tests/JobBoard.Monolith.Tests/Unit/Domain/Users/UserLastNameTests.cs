using Shouldly;
using JobBoard.Domain.ValueObjects.User;

namespace JobBoard.Monolith.Tests.Unit.Domain.Users;

[Trait("Category", "Unit")]
public class UserLastNameTests
{
    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        var result = UserLastName.Create("Doe");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("Doe");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = UserLastName.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("LastName.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = UserLastName.Create(new string('a', 101));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("LastName.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = UserLastName.Create(new string('a', 100));

        result.IsSuccess.ShouldBeTrue();
    }
}
