using Shouldly;
using JobBoard.Domain.ValueObjects.User;

namespace JobBoard.Monolith.Tests.Unit.Domain.Users;

[Trait("Category", "Unit")]
public class UserFirstNameTests
{
    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        var result = UserFirstName.Create("John");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("John");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = UserFirstName.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("FirstName.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = UserFirstName.Create(new string('a', 101));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("FirstName.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = UserFirstName.Create(new string('a', 100));

        result.IsSuccess.ShouldBeTrue();
    }
}
