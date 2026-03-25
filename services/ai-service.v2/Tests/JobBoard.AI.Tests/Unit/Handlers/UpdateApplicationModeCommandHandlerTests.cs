using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Interfaces.Configurations;
using AppUnit = JobBoard.AI.Application.Interfaces.Configurations.Unit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class UpdateApplicationModeCommandHandlerTests
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly UpdateApplicationModeCommandHandler _sut;

    public UpdateApplicationModeCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);

        _sut = new UpdateApplicationModeCommandHandler(handlerContext, _settingsService);
    }

    [Fact]
    public async Task HandleAsync_UpdatesToMonolithMode_CallsSettingsService()
    {
        // Arrange
        var modeDto = new ApplicationModeDto { IsMonolith = true };
        var command = new UpdateApplicationModeCommand(modeDto);

        _settingsService.UpdateApplicationModeAsync(Arg.Any<ApplicationModeDto>())
            .Returns(AppUnit.Value);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(modeDto);
        result.IsMonolith.ShouldBeTrue();
        await _settingsService.Received(1).UpdateApplicationModeAsync(
            Arg.Is<ApplicationModeDto>(d => d.IsMonolith));
    }

    [Fact]
    public async Task HandleAsync_UpdatesToMicroservicesMode_CallsSettingsService()
    {
        // Arrange
        var modeDto = new ApplicationModeDto { IsMonolith = false };
        var command = new UpdateApplicationModeCommand(modeDto);

        _settingsService.UpdateApplicationModeAsync(Arg.Any<ApplicationModeDto>())
            .Returns(AppUnit.Value);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(modeDto);
        result.IsMonolith.ShouldBeFalse();
        await _settingsService.Received(1).UpdateApplicationModeAsync(
            Arg.Is<ApplicationModeDto>(d => !d.IsMonolith));
    }

    [Fact]
    public async Task HandleAsync_ReturnsTheRequestDto()
    {
        // Arrange
        var modeDto = new ApplicationModeDto { IsMonolith = true };
        var command = new UpdateApplicationModeCommand(modeDto);

        _settingsService.UpdateApplicationModeAsync(Arg.Any<ApplicationModeDto>())
            .Returns(AppUnit.Value);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — handler returns request.Request, not a new object
        result.ShouldBeSameAs(modeDto);
    }
}
