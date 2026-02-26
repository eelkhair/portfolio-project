using JobBoard.Application.Actions.Settings.Provider;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class GetProviderQueryHandlerTests
{
    private readonly IAiServiceClient _aiServiceClient;
    private readonly GetProviderQueryHandler _sut;

    public GetProviderQueryHandlerTests()
    {
        var context = Substitute.For<IJobBoardQueryDbContext, ITransactionDbContext>();
        var changeTracker = new StubQueryDbContext().ChangeTracker;
        ((ITransactionDbContext)context).ChangeTracker.Returns(changeTracker);

        _aiServiceClient = Substitute.For<IAiServiceClient>();

        _sut = new GetProviderQueryHandler(
            context,
            Substitute.For<ILogger<GetProviderQueryHandler>>(),
            _aiServiceClient);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAiServiceClient()
    {
        _aiServiceClient.GetProvider(Arg.Any<CancellationToken>())
            .Returns(new ProviderSettings { Provider = "openai", Model = "gpt-4.1-mini" });

        await _sut.HandleAsync(new GetProviderQuery(), CancellationToken.None);

        await _aiServiceClient.Received(1).GetProvider(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnProviderSettings()
    {
        var expected = new ProviderSettings { Provider = "anthropic", Model = "claude-sonnet-4-5-20250929" };
        _aiServiceClient.GetProvider(Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.HandleAsync(new GetProviderQuery(), CancellationToken.None);

        result.ShouldBe(expected);
        result.Provider.ShouldBe("anthropic");
        result.Model.ShouldBe("claude-sonnet-4-5-20250929");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDefaultProviderValues()
    {
        var settings = new ProviderSettings();
        _aiServiceClient.GetProvider(Arg.Any<CancellationToken>())
            .Returns(settings);

        var result = await _sut.HandleAsync(new GetProviderQuery(), CancellationToken.None);

        result.Provider.ShouldBe("openai");
        result.Model.ShouldBe("gpt-4.1-mini");
    }

    [Fact]
    public async Task HandleAsync_WhenAiServiceThrows_ShouldPropagateException()
    {
        _aiServiceClient.GetProvider(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProviderSettings>(new HttpRequestException("Service unavailable")));

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.HandleAsync(new GetProviderQuery(), CancellationToken.None));
    }
}
