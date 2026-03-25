using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetApplicationModeQueryHandlerTests
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly GetApplicationModeQueryHandler _sut;

    public GetApplicationModeQueryHandlerTests()
    {
        var logger = Substitute.For<ILogger<GetApplicationModeQuery>>();
        _sut = new GetApplicationModeQueryHandler(logger, _settingsService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMonolithMode()
    {
        // Arrange
        var expected = new ApplicationModeDto { IsMonolith = true };
        _settingsService.GetApplicationModeAsync().Returns(expected);

        // Act
        var result = await _sut.HandleAsync(new GetApplicationModeQuery(), CancellationToken.None);

        // Assert
        result.IsMonolith.ShouldBeTrue();
        await _settingsService.Received(1).GetApplicationModeAsync();
    }

    [Fact]
    public async Task HandleAsync_ReturnsMicroservicesMode()
    {
        // Arrange
        var expected = new ApplicationModeDto { IsMonolith = false };
        _settingsService.GetApplicationModeAsync().Returns(expected);

        // Act
        var result = await _sut.HandleAsync(new GetApplicationModeQuery(), CancellationToken.None);

        // Assert
        result.IsMonolith.ShouldBeFalse();
    }
}
