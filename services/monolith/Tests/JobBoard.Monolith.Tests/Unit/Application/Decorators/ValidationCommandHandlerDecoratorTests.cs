using FluentValidation;
using FluentValidation.Results;
using JobBoard.Application.Infrastructure.Decorators;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System.Diagnostics;
using JobBoard.Monolith.Tests.Unit.Application.Helpers;

namespace JobBoard.Monolith.Tests.Unit.Application.Decorators;

[Trait("Category", "Unit")]
public class ValidationCommandHandlerDecoratorTests
{
    private readonly IHandler<TestCommand, string> _innerHandler;
    private readonly IActivityFactory _activityFactory;

    public ValidationCommandHandlerDecoratorTests()
    {
        _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
        _activityFactory = Substitute.For<IActivityFactory>();
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);
    }

    [Fact]
    public async Task HandleAsync_WhenNoValidator_ShouldCallInnerHandler()
    {
        var sut = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, validator: null);
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns("ok");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ShouldBe("ok");
        await _innerHandler.Received(1).HandleAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenValidationPasses_ShouldCallInnerHandler()
    {
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var sut = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, validator);
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns("ok");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ShouldBe("ok");
        await _innerHandler.Received(1).HandleAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShouldThrowValidationException()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));
        var sut = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, validator);
        var request = new TestCommand { Name = "" };

        var ex = await Should.ThrowAsync<ValidationException>(
            () => sut.HandleAsync(request, CancellationToken.None));

        ex.Errors.ShouldContain(e => e.PropertyName == "Name");
        await _innerHandler.DidNotReceive().HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenValidatorProvided_ShouldStartActivity()
    {
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var sut = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, validator);
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>()).Returns("ok");

        await sut.HandleAsync(new TestCommand(), CancellationToken.None);

        _activityFactory.Received(1).StartActivity(
            "TestCommand.validate",
            ActivityKind.Internal,
            Arg.Any<ActivityContext>());
    }
}
