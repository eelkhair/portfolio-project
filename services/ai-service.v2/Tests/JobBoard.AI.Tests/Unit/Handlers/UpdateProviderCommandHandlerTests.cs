using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;
using AppUnit = JobBoard.AI.Application.Interfaces.Configurations.Unit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class UpdateProviderCommandHandlerTests
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly UpdateProviderCommandHandler _sut;

    public UpdateProviderCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);

        _sut = new UpdateProviderCommandHandler(handlerContext, _settingsService);
    }

    [Fact]
    public async Task HandleAsync_CallsSettingsServiceWithCorrectRequest()
    {
        // Arrange
        var providerRequest = new UpdateProviderRequest
        {
            Provider = "anthropic",
            Model = "claude-3-opus"
        };
        var command = new UpdateProviderCommand(providerRequest);

        _settingsService.UpdateProviderAsync(Arg.Any<UpdateProviderRequest>())
            .Returns(AppUnit.Value);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(AppUnit.Value);
        await _settingsService.Received(1).UpdateProviderAsync(
            Arg.Is<UpdateProviderRequest>(r => r.Provider == "anthropic" && r.Model == "claude-3-opus"));
    }

    [Fact]
    public async Task HandleAsync_WithDefaultValues_CallsSettingsService()
    {
        // Arrange
        var providerRequest = new UpdateProviderRequest();
        var command = new UpdateProviderCommand(providerRequest);

        _settingsService.UpdateProviderAsync(Arg.Any<UpdateProviderRequest>())
            .Returns(AppUnit.Value);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(AppUnit.Value);
        await _settingsService.Received(1).UpdateProviderAsync(
            Arg.Is<UpdateProviderRequest>(r => r.Provider == "openai" && r.Model == "gpt-4.1-mini"));
    }
}
