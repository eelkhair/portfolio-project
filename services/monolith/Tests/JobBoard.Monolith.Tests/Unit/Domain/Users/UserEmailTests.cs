using Shouldly;
using JobBoard.Domain.ValueObjects.User;

namespace JobBoard.Monolith.Tests.Unit.Domain.Users;

[Trait("Category", "Unit")]
public class UserEmailTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldSucceed()
    {
        var result = UserEmail.Create("john@example.com");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("john@example.com");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimValue()
    {
        var result = UserEmail.Create("  john@example.com  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("john@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldFail(string? value)
    {
        var result = UserEmail.Create(value!);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("Email.Empty");
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldFail()
    {
        var longEmail = new string('a', 250) + "@ab.com";

        var result = UserEmail.Create(longEmail);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == "Email.TooLong");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@no-local.com")]
    public void Create_WithInvalidFormat_ShouldFail(string value)
    {
        var result = UserEmail.Create(value);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == "Email.InvalidFormat");
    }
}
