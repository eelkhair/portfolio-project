using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Shared;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GenerateDraftCommandHandlerTests
{
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IAiPrompt<GenerateDraftRequest> _aiPrompt = Substitute.For<IAiPrompt<GenerateDraftRequest>>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly DraftGenCommandHandler _sut;

    public GenerateDraftCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        _sut = new DraftGenCommandHandler(handlerContext, _aiPrompt, _activityFactory, _chatService);
    }

    [Fact]
    public async Task HandleAsync_BuildsPromptAndCallsChatService()
    {
        // Arrange
        var request = new GenerateDraftRequest
        {
            Brief = "Senior .NET developer",
            RoleLevel = RoleLevel.Senior,
            Tone = Tone.Neutral,
            CompanyName = "Acme Corp",
            Location = "Remote"
        };
        var command = new GenerateDraftCommand(Guid.NewGuid(), request);

        _aiPrompt.BuildUserPrompt(request).Returns("user prompt text");
        _aiPrompt.BuildSystemPrompt().Returns("system prompt text");
        _aiPrompt.AllowTools.Returns(false);
        _aiPrompt.Name.Returns("GenerateJob");
        _aiPrompt.Version.Returns("0.1");

        var expectedResponse = new DraftResponse
        {
            Title = "Senior .NET Developer",
            AboutRole = "We are looking for a senior developer."
        };

        _chatService.GetResponseAsync<DraftResponse>("system prompt text", "user prompt text", false, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
        result.Title.ShouldBe("Senior .NET Developer");
        _aiPrompt.Received(1).BuildUserPrompt(request);
        _aiPrompt.Received(1).BuildSystemPrompt();
    }

    [Fact]
    public async Task HandleAsync_PassesAllowToolsFromPrompt()
    {
        // Arrange
        var request = new GenerateDraftRequest { Brief = "Test brief" };
        var command = new GenerateDraftCommand(Guid.NewGuid(), request);

        _aiPrompt.BuildUserPrompt(Arg.Any<GenerateDraftRequest>()).Returns("user");
        _aiPrompt.BuildSystemPrompt().Returns("system");
        _aiPrompt.AllowTools.Returns(false);
        _aiPrompt.Name.Returns("GenerateJob");
        _aiPrompt.Version.Returns("0.1");

        _chatService.GetResponseAsync<DraftResponse>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new DraftResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _chatService.Received(1).GetResponseAsync<DraftResponse>(
            "system",
            "user",
            false,
            Arg.Any<CancellationToken>());
    }
}
