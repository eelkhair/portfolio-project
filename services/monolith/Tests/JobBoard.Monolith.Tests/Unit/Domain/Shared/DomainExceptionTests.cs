using Shouldly;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Monolith.Tests.Unit.Domain.Shared;

[Trait("Category", "Unit")]
public class DomainExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetErrorCode()
    {
        var errors = new List<Error> { new("Test.Error", "Something went wrong") };

        var exception = new DomainException("Test.Code", errors);

        exception.Message.ShouldContain("Test.Code");
    }

    [Fact]
    public void Constructor_ShouldSetErrors()
    {
        var errors = new List<Error>
        {
            new("Error1", "First error"),
            new("Error2", "Second error")
        };

        var exception = new DomainException("Test.Code", errors);

        exception.Errors.Count().ShouldBe(2);
        exception.Errors.ShouldContain(e => e.Code == "Error1");
        exception.Errors.ShouldContain(e => e.Code == "Error2");
    }
}
