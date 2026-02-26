using JobBoard.Application.Actions.Settings.Provider;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Monolith.Contracts.Settings;
using JobBoard.Monolith.Tests.Unit.Application.Decorators;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class UpdateProviderCommandHandlerTests
{
    private readonly IAiServiceClient _aiServiceClient;
    private readonly UpdateProviderCommandHandler _sut;

    public UpdateProviderCommandHandlerTests()
    {
        var unitOfWork = Substitute.For<IUnitOfWork, ITransactionDbContext>();
        var changeTracker = new StubDbContext().ChangeTracker;
        ((ITransactionDbContext)unitOfWork).ChangeTracker.Returns(changeTracker);

        _aiServiceClient = Substitute.For<IAiServiceClient>();

        var handlerContext = Substitute.For<IHandlerContext>();
        handlerContext.UnitOfWork.Returns(unitOfWork);
        handlerContext.OutboxPublisher.Returns(Substitute.For<IOutboxPublisher>());
        handlerContext.MetricsService.Returns(Substitute.For<IMetricsService>());
        handlerContext.UnitOfWorkEvents.Returns(Substitute.For<IUnitOfWorkEvents>());
        handlerContext.LoggerFactory.Returns(Substitute.For<ILoggerFactory>());

        _sut = new UpdateProviderCommandHandler(handlerContext, _aiServiceClient);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAiServiceClient()
    {
        var command = new UpdateProviderCommand
        {
            Request = new UpdateProviderRequest { Provider = "anthropic", Model = "claude-sonnet-4-5-20250929" },
            UserId = "user-123"
        };

        await _sut.HandleAsync(command, CancellationToken.None);

        await _aiServiceClient.Received(1).UpdateProvider(
            Arg.Is<UpdateProviderRequest>(r => r.Provider == "anthropic" && r.Model == "claude-sonnet-4-5-20250929"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTrue()
    {
        var command = new UpdateProviderCommand
        {
            Request = new UpdateProviderRequest { Provider = "openai", Model = "gpt-4.1-mini" },
            UserId = "user-123"
        };

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldImplementINoTransaction()
    {
        var command = new UpdateProviderCommand();

        command.ShouldBeAssignableTo<INoTransaction>();
    }

    [Fact]
    public async Task HandleAsync_WhenAiServiceThrows_ShouldPropagateException()
    {
        var command = new UpdateProviderCommand
        {
            Request = new UpdateProviderRequest { Provider = "openai", Model = "gpt-4.1" },
            UserId = "user-123"
        };
        _aiServiceClient.UpdateProvider(Arg.Any<UpdateProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Connection refused")));

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WithDefaultValues_ShouldPassDefaults()
    {
        var command = new UpdateProviderCommand
        {
            Request = new UpdateProviderRequest(),
            UserId = "user-123"
        };

        await _sut.HandleAsync(command, CancellationToken.None);

        await _aiServiceClient.Received(1).UpdateProvider(
            Arg.Is<UpdateProviderRequest>(r => r.Provider == "openai" && r.Model == "gpt-4.1-mini"),
            Arg.Any<CancellationToken>());
    }
}
