using System.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Infrastructure.Decorators;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Decorators;

// Test command for decorator tests
public class TestCommand : BaseCommand<string>
{
    public string Input { get; set; } = string.Empty;
}

[Trait("Category", "Unit")]
public class ValidationCommandHandlerDecoratorTests
{
    private readonly IHandler<TestCommand, string> _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();

    public ValidationCommandHandlerDecoratorTests()
    {
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);
    }

    [Fact]
    public async Task HandleAsync_NoValidator_CallsInnerHandler()
    {
        // Arrange
        var decorator = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, null);

        var command = new TestCommand { Input = "test" };
        _innerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns("result");

        // Act
        var result = await decorator.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe("result");
        await _innerHandler.Received(1).HandleAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CallsInnerHandler()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var decorator = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, validator);

        var command = new TestCommand { Input = "valid" };
        _innerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns("success");

        // Act
        var result = await decorator.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe("success");
        await _innerHandler.Received(1).HandleAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestCommand>>();
        var errors = new List<ValidationFailure>
        {
            new("Input", "Input is required")
        };
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var decorator = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, validator);

        var command = new TestCommand { Input = "" };

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(
            () => decorator.HandleAsync(command, CancellationToken.None));

        await _innerHandler.DidNotReceive().HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MultipleValidationErrors_ThrowsWithAllErrors()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestCommand>>();
        var errors = new List<ValidationFailure>
        {
            new("Input", "Input is required"),
            new("Input", "Input must be at least 3 characters")
        };
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var decorator = new ValidationCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(
            () => decorator.HandleAsync(new TestCommand(), CancellationToken.None));

        ex.Errors.Count().ShouldBe(2);
    }
}
