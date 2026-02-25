using Shouldly;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Monolith.Tests.Unit.Domain.Shared;

[Trait("Category", "Unit")]
public class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccess()
    {
        var result = Result<string>.Success("value");

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe("value");
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_ShouldBeFailure()
    {
        var errors = new List<Error> { new("Code", "Description") };

        var result = Result<string>.Failure(errors);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem();
    }

    [Fact]
    public void Failure_ShouldContainAllErrors()
    {
        var errors = new List<Error>
        {
            new("Error1", "First"),
            new("Error2", "Second"),
            new("Error3", "Third")
        };

        var result = Result<string>.Failure(errors);

        result.Errors.Count().ShouldBe(3);
    }

    [Fact]
    public void Failure_ValueShouldBeDefault()
    {
        var errors = new List<Error> { new("Code", "Description") };

        var result = Result<string>.Failure(errors);

        result.Value.ShouldBeNull();
    }
}
