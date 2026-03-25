using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetProviderQueryHandlerTests
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly GetProviderQueryHandler _sut;

    public GetProviderQueryHandlerTests()
    {
        var logger = Substitute.For<ILogger<GetProviderQuery>>();
        _sut = new GetProviderQueryHandler(logger, _settingsService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsProviderFromSettingsService()
    {
        // Arrange
        var expected = new GetProviderResponse { Provider = "anthropic", Model = "claude-3-opus" };
        _settingsService.GetProviderAsync().Returns(expected);

        // Act
        var result = await _sut.HandleAsync(new GetProviderQuery(), CancellationToken.None);

        // Assert
        result.Provider.ShouldBe("anthropic");
        result.Model.ShouldBe("claude-3-opus");
        await _settingsService.Received(1).GetProviderAsync();
    }

    [Fact]
    public async Task HandleAsync_ReturnsDefaultValues()
    {
        // Arrange
        var expected = new GetProviderResponse(); // defaults: openai, gpt-4.1-mini
        _settingsService.GetProviderAsync().Returns(expected);

        // Act
        var result = await _sut.HandleAsync(new GetProviderQuery(), CancellationToken.None);

        // Assert
        result.Provider.ShouldBe("openai");
        result.Model.ShouldBe("gpt-4.1-mini");
    }
}
