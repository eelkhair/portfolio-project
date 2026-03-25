using JobBoard.AI.Domain.Exceptions;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class ResultTests
{
    [Fact]
    public void Success_ReturnsSuccessResult()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe("hello");
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_ReturnsFailureResult()
    {
        var errors = new List<Error> { new("E001", "Something went wrong") };
        var result = Result<string>.Failure(errors);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Code.ShouldBe("E001");
        result.Errors.First().Description.ShouldBe("Something went wrong");
    }

    [Fact]
    public void Failure_WithMultipleErrors_ContainsAllErrors()
    {
        var errors = new List<Error>
        {
            new("E001", "First error"),
            new("E002", "Second error"),
            new("E003", "Third error")
        };
        var result = Result<int>.Failure(errors);

        result.Errors.Count.ShouldBe(3);
    }

    [Fact]
    public void Success_ValueIsNull_StillIsSuccess()
    {
        var result = Result<string?>.Success(null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }
}

[Trait("Category", "Unit")]
public class ErrorTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var error = new Error("CODE", "Description");

        error.Code.ShouldBe("CODE");
        error.Description.ShouldBe("Description");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var e1 = new Error("E001", "Desc");
        var e2 = new Error("E001", "Desc");
        e1.ShouldBe(e2);
    }

    [Fact]
    public void Equality_DifferentValues_NotEqual()
    {
        var e1 = new Error("E001", "Desc1");
        var e2 = new Error("E002", "Desc2");
        e1.ShouldNotBe(e2);
    }
}

[Trait("Category", "Unit")]
public class DomainExceptionTests
{
    [Fact]
    public void Constructor_SetsErrorsAndMessage()
    {
        var errors = new List<Error> { new("E001", "Validation failed") };
        var exception = new DomainException("VALIDATION", errors);

        exception.Errors.Count.ShouldBe(1);
        exception.Errors.First().Code.ShouldBe("E001");
        exception.Message.ShouldContain("VALIDATION");
    }

    [Fact]
    public void Constructor_IsException()
    {
        var exception = new DomainException("CODE", new List<Error>());
        exception.ShouldBeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithMultipleErrors_StoresAll()
    {
        var errors = new List<Error>
        {
            new("E001", "Error 1"),
            new("E002", "Error 2")
        };
        var exception = new DomainException("MULTI", errors);

        exception.Errors.Count.ShouldBe(2);
    }
}
