using FluentValidation;
using JobBoard.Application.Infrastructure.Decorators;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using JobBoard.Monolith.Tests.Unit.Application.Helpers;

namespace JobBoard.Monolith.Tests.Unit.Application.Decorators;

[Trait("Category", "Unit")]
public class ObservabilityCommandHandlerDecoratorTests
{
    private readonly IHandler<TestCommand, string> _innerHandler;
    private readonly ILogger<TestCommand> _logger;
    private readonly IUserAccessor _userAccessor;
    private readonly IMetricsService _metricsService;
    private readonly ObservabilityCommandHandlerDecorator<TestCommand, string> _sut;

    public ObservabilityCommandHandlerDecoratorTests()
    {
        _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
        _logger = Substitute.For<ILogger<TestCommand>>();
        _userAccessor = Substitute.For<IUserAccessor>();
        _metricsService = Substitute.For<IMetricsService>();
        _sut = new ObservabilityCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _logger, _userAccessor, _metricsService);
    }

    [Fact]
    public async Task HandleAsync_OnSuccess_ShouldIncrementSuccessMetric()
    {
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>()).Returns("ok");

        await _sut.HandleAsync(new TestCommand(), CancellationToken.None);

        _metricsService.Received(1).IncrementCommandSuccess("TestCommand");
    }

    [Fact]
    public async Task HandleAsync_OnNonValidationException_ShouldIncrementFailureMetric()
    {
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new TestCommand(), CancellationToken.None));

        _metricsService.Received(1).IncrementCommandFailure("TestCommand");
        _metricsService.DidNotReceive().IncrementCommandSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_OnValidationException_ShouldRethrowWithoutFailureMetric()
    {
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Throws(new ValidationException("validation failed"));

        await Should.ThrowAsync<ValidationException>(
            () => _sut.HandleAsync(new TestCommand(), CancellationToken.None));

        _metricsService.DidNotReceive().IncrementCommandFailure(Arg.Any<string>());
        _metricsService.DidNotReceive().IncrementCommandSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WithQueryType_ShouldStillIncrementSuccessMetric()
    {
        var queryHandler = Substitute.For<IHandler<TestQuery, string>>();
        var queryLogger = Substitute.For<ILogger<TestQuery>>();
        queryHandler.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>()).Returns("result");

        var sut = new ObservabilityCommandHandlerDecorator<TestQuery, string>(
            queryHandler, queryLogger, _userAccessor, _metricsService);

        await sut.HandleAsync(new TestQuery(), CancellationToken.None);

        _metricsService.Received(1).IncrementCommandSuccess("TestQuery");
    }
}
