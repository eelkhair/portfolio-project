using FluentValidation;
using JobBoard.AI.Application.Infrastructure.Decorators;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Decorators;

[Trait("Category", "Unit")]
public class ObservabilityCommandHandlerDecoratorTests
{
    private readonly IHandler<TestCommand, string> _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
    private readonly ILogger<TestCommand> _logger = Substitute.For<ILogger<TestCommand>>();
    private readonly IMetricsService _metricsService = Substitute.For<IMetricsService>();
    private readonly ObservabilityCommandHandlerDecorator<TestCommand, string> _sut;

    public ObservabilityCommandHandlerDecoratorTests()
    {
        _sut = new ObservabilityCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _logger, _metricsService);
    }

    [Fact]
    public async Task HandleAsync_Success_IncrementsSuccessMetric()
    {
        // Arrange
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns("result");

        // Act
        var result = await _sut.HandleAsync(new TestCommand(), CancellationToken.None);

        // Assert
        result.ShouldBe("result");
        _metricsService.Received(1).IncrementCommandSuccess("TestCommand");
    }

    [Fact]
    public async Task HandleAsync_Failure_IncrementsFailureMetricAndRethrows()
    {
        // Arrange
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Something broke"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new TestCommand(), CancellationToken.None));

        _metricsService.Received(1).IncrementCommandFailure("TestCommand");
    }

    [Fact]
    public async Task HandleAsync_ValidationException_DoesNotIncrementFailureMetric()
    {
        // Arrange
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Throws(new ValidationException("Validation failed"));

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(
            () => _sut.HandleAsync(new TestCommand(), CancellationToken.None));

        _metricsService.DidNotReceive().IncrementCommandFailure(Arg.Any<string>());
        _metricsService.DidNotReceive().IncrementCommandSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_CallsInnerHandler()
    {
        // Arrange
        var command = new TestCommand { Input = "test" };
        _innerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns("ok");

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _innerHandler.Received(1).HandleAsync(command, Arg.Any<CancellationToken>());
    }
}
