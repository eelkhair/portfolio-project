using Shouldly;
using JobBoard.Domain.ValueObjects.User;

namespace JobBoard.Monolith.Tests.Unit.Domain.Users;

[Trait("Category", "Unit")]
public class UserExternalIdTests
{
    [Fact]
    public void Create_WithValidId_ShouldSucceed()
    {
        var result = UserExternalId.Create("auth0|abc123");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("auth0|abc123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldSucceedWithNull(string? value)
    {
        var result = UserExternalId.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBeNull();
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var result = UserExternalId.Create(new string('a', 101));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("UserExternalId.TooLong");
    }

    [Fact]
    public void Create_AtMaxLength_ShouldSucceed()
    {
        var result = UserExternalId.Create(new string('a', 100));

        result.IsSuccess.ShouldBeTrue();
    }
}
