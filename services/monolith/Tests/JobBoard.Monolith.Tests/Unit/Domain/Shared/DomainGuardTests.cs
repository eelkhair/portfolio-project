using Shouldly;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;

namespace JobBoard.Monolith.Tests.Unit.Domain.Shared;

[Trait("Category", "Unit")]
public class DomainGuardTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AgainstInvalidId_Long_WithInvalidId_ShouldAddError(long id)
    {
        var errors = new List<Error>();

        DomainGuard.AgainstInvalidId(id, "Test.InvalidId", errors);

        errors.ShouldHaveSingleItem().Code.ShouldBe("Test.InvalidId");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void AgainstInvalidId_Long_WithValidId_ShouldNotAddError(long id)
    {
        var errors = new List<Error>();

        DomainGuard.AgainstInvalidId(id, "Test.InvalidId", errors);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void AgainstInvalidId_Guid_WithEmptyGuid_ShouldAddError()
    {
        var errors = new List<Error>();

        DomainGuard.AgainstInvalidId(Guid.Empty, "Test.InvalidGuid", errors);

        errors.ShouldHaveSingleItem().Code.ShouldBe("Test.InvalidGuid");
    }

    [Fact]
    public void AgainstInvalidId_Guid_WithValidGuid_ShouldNotAddError()
    {
        var errors = new List<Error>();

        DomainGuard.AgainstInvalidId(Guid.NewGuid(), "Test.InvalidGuid", errors);

        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrEmpty_WithInvalidValue_ShouldAddError(string? value)
    {
        var errors = new List<Error>();

        DomainGuard.AgainstNullOrEmpty(value, "Test.Empty", errors);

        errors.ShouldHaveSingleItem().Code.ShouldBe("Test.Empty");
    }

    [Fact]
    public void AgainstNullOrEmpty_WithValidValue_ShouldNotAddError()
    {
        var errors = new List<Error>();

        DomainGuard.AgainstNullOrEmpty("valid", "Test.Empty", errors);

        errors.ShouldBeEmpty();
    }
}
