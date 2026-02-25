using FluentValidation;
using JobBoard.Application.Infrastructure.Decorators;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using JobBoard.Monolith.Tests.Unit.Application.Helpers;

namespace JobBoard.Monolith.Tests.Unit.Application.Decorators;

[Trait("Category", "Unit")]
public class ExceptionHandlingCommandHandlerDecoratorTests
{
    private readonly IHandler<TestCommand, string> _innerHandler;
    private readonly ExceptionHandlingCommandHandlerDecorator<TestCommand, string> _sut;

    public ExceptionHandlingCommandHandlerDecoratorTests()
    {
        _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
        _sut = new ExceptionHandlingCommandHandlerDecorator<TestCommand, string>(_innerHandler);
    }

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerSucceeds_ShouldReturnResult()
    {
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns("success");

        var result = await _sut.HandleAsync(request, CancellationToken.None);

        result.ShouldBe("success");
    }

    [Fact]
    public async Task HandleAsync_WhenDomainExceptionThrown_ShouldThrowValidationException()
    {
        var request = new TestCommand { Name = "Test" };
        var errors = new List<Error>
        {
            new("Name.Required", "Company name is required"),
            new("Email.Invalid", "Email is invalid")
        };
        var domainException = new DomainException("Company", errors);
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Throws(domainException);

        var ex = await Should.ThrowAsync<ValidationException>(
            () => _sut.HandleAsync(request, CancellationToken.None));

        ex.Errors.Count().ShouldBe(2);
        ex.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Company name is required");
        ex.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorMessage == "Email is invalid");
    }

    [Fact]
    public async Task HandleAsync_WhenDomainExceptionThrown_ShouldSplitErrorCodeOnDot()
    {
        var request = new TestCommand { Name = "Test" };
        var errors = new List<Error>
        {
            new("CompanyName.TooLong", "Name is too long")
        };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>())
            .Throws(new DomainException("Validation", errors));

        var ex = await Should.ThrowAsync<ValidationException>(
            () => _sut.HandleAsync(request, CancellationToken.None));

        ex.Errors.First().PropertyName.ShouldBe("CompanyName");
    }

    [Fact]
    public async Task HandleAsync_WhenOtherExceptionThrown_ShouldRethrow()
    {
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Something broke"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.HandleAsync(request, CancellationToken.None));
    }
}
