using JobBoard.Application.Infrastructure.Exceptions;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Exceptions;

[Trait("Category", "Unit")]
public class ApplicationExceptionTests
{
    [Fact]
    public void NotFoundException_WithResourceAndKey_ShouldFormatMessage()
    {
        var ex = new NotFoundException("Company", Guid.Empty);

        ex.Message.ShouldContain("Company");
        ex.Message.ShouldContain(Guid.Empty.ToString());
        ex.ResourceName.ShouldBe("Company");
        ex.Key.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void NotFoundException_DefaultConstructor_ShouldWork()
    {
        var ex = new NotFoundException();

        ex.ResourceName.ShouldBeNull();
        ex.Key.ShouldBeNull();
    }

    [Fact]
    public void NotFoundException_WithMessage_ShouldSetMessage()
    {
        var ex = new NotFoundException("Custom message");

        ex.Message.ShouldBe("Custom message");
    }

    [Fact]
    public void NotFoundException_WithInnerException_ShouldPreserveIt()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new NotFoundException("outer", inner);

        ex.InnerException.ShouldBe(inner);
        ex.Message.ShouldBe("outer");
    }

    [Fact]
    public void NotFoundException_ShouldBeException()
    {
        var ex = new NotFoundException("Resource", 42);

        ex.ShouldBeAssignableTo<Exception>();
    }

    [Fact]
    public void ForbiddenAccessException_WithResourceAndKey_ShouldFormatMessage()
    {
        var ex = new ForbiddenAccessException("Company", 123);

        ex.Message.ShouldContain("Company");
        ex.Message.ShouldContain("123");
        ex.ResourceName.ShouldBe("Company");
        ex.Key.ShouldBe(123);
    }

    [Fact]
    public void ForbiddenAccessException_DefaultConstructor_ShouldWork()
    {
        var ex = new ForbiddenAccessException();

        ex.ResourceName.ShouldBeNull();
        ex.Key.ShouldBeNull();
    }

    [Fact]
    public void ForbiddenAccessException_WithMessage_ShouldSetMessage()
    {
        var ex = new ForbiddenAccessException("Access denied");

        ex.Message.ShouldBe("Access denied");
    }

    [Fact]
    public void ForbiddenAccessException_WithInnerException_ShouldPreserveIt()
    {
        var inner = new UnauthorizedAccessException("inner");
        var ex = new ForbiddenAccessException("outer", inner);

        ex.InnerException.ShouldBe(inner);
    }

    [Fact]
    public void ForbiddenAccessException_ShouldBeException()
    {
        var ex = new ForbiddenAccessException("Resource", "key");

        ex.ShouldBeAssignableTo<Exception>();
    }
}
