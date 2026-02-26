using JobBoard.Application.Actions.Settings.ApplicationMode;
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
public class UpdateApplicationModeCommandHandlerTests
{
    private readonly IAiServiceClient _aiServiceClient;
    private readonly UpdateApplicationModeCommandHandler _sut;

    public UpdateApplicationModeCommandHandlerTests()
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

        _sut = new UpdateApplicationModeCommandHandler(handlerContext, _aiServiceClient);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAiServiceClient()
    {
        var dto = new ApplicationModeDto { IsMonolith = true };
        var command = new UpdateApplicationModeCommand(dto) { UserId = "user-123" };

        await _sut.HandleAsync(command, CancellationToken.None);

        await _aiServiceClient.Received(1).UpdateApplicationMode(
            Arg.Is<ApplicationModeDto>(d => d.IsMonolith),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheRequestDto()
    {
        var dto = new ApplicationModeDto { IsMonolith = false };
        var command = new UpdateApplicationModeCommand(dto) { UserId = "user-123" };

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.ShouldBe(dto);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldPassCorrectIsMonolithValue(bool isMonolith)
    {
        var dto = new ApplicationModeDto { IsMonolith = isMonolith };
        var command = new UpdateApplicationModeCommand(dto) { UserId = "user-123" };

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.IsMonolith.ShouldBe(isMonolith);
    }

    [Fact]
    public async Task HandleAsync_WhenAiServiceThrows_ShouldPropagateException()
    {
        var dto = new ApplicationModeDto { IsMonolith = true };
        var command = new UpdateApplicationModeCommand(dto) { UserId = "user-123" };
        _aiServiceClient.UpdateApplicationMode(Arg.Any<ApplicationModeDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Service unavailable")));

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.HandleAsync(command, CancellationToken.None));
    }
}
