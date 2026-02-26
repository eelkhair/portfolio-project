using JobBoard.Application.Actions.Settings.ApplicationMode;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class GetApplicationModeQueryHandlerTests
{
    private readonly IAiServiceClient _aiServiceClient;
    private readonly GetApplicationModeQueryHandler _sut;

    public GetApplicationModeQueryHandlerTests()
    {
        var context = Substitute.For<IJobBoardQueryDbContext, ITransactionDbContext>();
        var changeTracker = new StubQueryDbContext().ChangeTracker;
        ((ITransactionDbContext)context).ChangeTracker.Returns(changeTracker);

        _aiServiceClient = Substitute.For<IAiServiceClient>();

        _sut = new GetApplicationModeQueryHandler(
            context,
            Substitute.For<ILogger<GetApplicationModeQueryHandler>>(),
            _aiServiceClient);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAiServiceClient()
    {
        _aiServiceClient.GetApplicationMode(Arg.Any<CancellationToken>())
            .Returns(new ApplicationModeDto { IsMonolith = true });

        await _sut.HandleAsync(new GetApplicationModeQuery(), CancellationToken.None);

        await _aiServiceClient.Received(1).GetApplicationMode(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnApplicationModeFromAiService()
    {
        var expected = new ApplicationModeDto { IsMonolith = true };
        _aiServiceClient.GetApplicationMode(Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.HandleAsync(new GetApplicationModeQuery(), CancellationToken.None);

        result.ShouldBe(expected);
        result.IsMonolith.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldReturnCorrectIsMonolithValue(bool isMonolith)
    {
        _aiServiceClient.GetApplicationMode(Arg.Any<CancellationToken>())
            .Returns(new ApplicationModeDto { IsMonolith = isMonolith });

        var result = await _sut.HandleAsync(new GetApplicationModeQuery(), CancellationToken.None);

        result.IsMonolith.ShouldBe(isMonolith);
    }

    [Fact]
    public async Task HandleAsync_WhenAiServiceThrows_ShouldPropagateException()
    {
        _aiServiceClient.GetApplicationMode(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ApplicationModeDto>(new HttpRequestException("Connection refused")));

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.HandleAsync(new GetApplicationModeQuery(), CancellationToken.None));
    }
}

/// <summary>
/// Minimal DbContext for query handler tests that need a real ChangeTracker.
/// </summary>
internal class StubQueryDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase($"QueryTests_{Guid.NewGuid()}");
}
