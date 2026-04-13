using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Drafts.RewriteItem;
using JobBoard.AI.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class RewriteItemCommandHandlerTests
{
    private readonly IAiPrompt<RewriteItemRequest> _aiPrompt = Substitute.For<IAiPrompt<RewriteItemRequest>>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly RewriteItemCommandHandler _sut;

    public RewriteItemCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        _sut = new RewriteItemCommandHandler(handlerContext, _aiPrompt, _chatService, _activityFactory);
    }

    [Fact]
    public async Task HandleAsync_CallsChatServiceWithPrompts()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Title,
            Value = "Software Engineer"
        };
        var command = new RewriteItemCommand(request);

        _aiPrompt.BuildSystemPrompt().Returns("system prompt");
        _aiPrompt.BuildUserPrompt(Arg.Any<RewriteItemRequest>()).Returns("user prompt");
        _aiPrompt.AllowTools.Returns(false);
        _aiPrompt.Name.Returns("rewrite");
        _aiPrompt.Version.Returns("1.0");

        var expectedResponse = new RewriteItemResponse
        {
            Field = RewriteField.Title,
            Options = ["Option 1", "Option 2", "Option 3"]
        };

        _chatService.GetResponseAsync<RewriteItemResponse>(
                "system prompt", "user prompt", false, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Options.Count.ShouldBe(3);
        result.Field.ShouldBe(RewriteField.Title);
    }

    [Fact]
    public async Task HandleAsync_SetsFieldFromRequest()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.AboutRole,
            Value = "We are looking for..."
        };
        var command = new RewriteItemCommand(request);

        _aiPrompt.BuildSystemPrompt().Returns("sys");
        _aiPrompt.BuildUserPrompt(Arg.Any<RewriteItemRequest>()).Returns("usr");
        _aiPrompt.AllowTools.Returns(false);
        _aiPrompt.Name.Returns("rewrite");
        _aiPrompt.Version.Returns("1.0");

        // Return response with wrong field to verify handler overrides it
        var response = new RewriteItemResponse
        {
            Field = RewriteField.Title, // intentionally wrong
            Options = ["Rewritten text"]
        };

        _chatService.GetResponseAsync<RewriteItemResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — handler overrides field to match request
        result.Field.ShouldBe(RewriteField.AboutRole);
    }

    [Fact]
    public async Task HandleAsync_PassesAllowToolsFromPrompt()
    {
        // Arrange
        var request = new RewriteItemRequest { Field = RewriteField.Responsibilities, Value = "Test" };
        var command = new RewriteItemCommand(request);

        _aiPrompt.BuildSystemPrompt().Returns("sys");
        _aiPrompt.BuildUserPrompt(Arg.Any<RewriteItemRequest>()).Returns("usr");
        _aiPrompt.AllowTools.Returns(true);
        _aiPrompt.Name.Returns("rewrite");
        _aiPrompt.Version.Returns("1.0");

        _chatService.GetResponseAsync<RewriteItemResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse { Options = [] });

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _chatService.Received(1).GetResponseAsync<RewriteItemResponse>(
            "sys", "usr", true, Arg.Any<CancellationToken>());
    }
}
