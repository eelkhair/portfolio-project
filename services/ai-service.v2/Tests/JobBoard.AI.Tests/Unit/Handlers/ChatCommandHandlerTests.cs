using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ChatCommandHandlerTests
{
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly IHandlerContext _handlerContext;
    private readonly ChatCommandHandler _sut;

    public ChatCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _handlerContext = new HandlerContext(loggerFactory);
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        _sut = new ChatCommandHandler(_handlerContext, _chatService, _activityFactory);
    }

    [Fact]
    public async Task HandleAsync_AdminScope_CallsChatServiceWithAdminPrompt()
    {
        // Arrange
        var command = new ChatCommand("Hello", null, Guid.NewGuid(), ChatScope.Admin);
        var expectedResponse = new ChatResponse { Response = "Hi", ConversationId = Guid.NewGuid() };

        _chatService.RunChatAsync(Arg.Any<string>(), Arg.Any<string>(), ChatScope.Admin, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
        await _chatService.Received(1).RunChatAsync(
            Arg.Is<string>(s => s.Contains("with tool access")),
            "Hello",
            ChatScope.Admin,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PublicScope_CallsChatServiceWithPublicPrompt()
    {
        // Arrange
        var command = new ChatCommand("Find me a job", null, Guid.NewGuid(), ChatScope.Public);
        var expectedResponse = new ChatResponse { Response = "Sure", ConversationId = Guid.NewGuid() };

        _chatService.RunChatAsync(Arg.Any<string>(), Arg.Any<string>(), ChatScope.Public, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
        await _chatService.Received(1).RunChatAsync(
            Arg.Is<string>(s => s.Contains("job board platform")),
            "Find me a job",
            ChatScope.Public,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompanyAdminScope_CallsChatServiceWithAdminPrompt()
    {
        // Arrange
        var command = new ChatCommand("List jobs", null, Guid.NewGuid(), ChatScope.CompanyAdmin);
        var expectedResponse = new ChatResponse { Response = "Here are jobs", ConversationId = Guid.NewGuid() };

        _chatService.RunChatAsync(Arg.Any<string>(), Arg.Any<string>(), ChatScope.CompanyAdmin, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
        await _chatService.Received(1).RunChatAsync(
            Arg.Is<string>(s => s.Contains("with tool access")),
            Arg.Any<string>(),
            ChatScope.CompanyAdmin,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithCompanyId_PrependsContextToMessage()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var command = new ChatCommand("Show details", companyId, Guid.NewGuid(), ChatScope.Admin);
        var expectedResponse = new ChatResponse { Response = "Details", ConversationId = Guid.NewGuid() };

        _chatService.RunChatAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ChatScope>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _chatService.Received(1).RunChatAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains($"companyId: {companyId}") && s.Contains("Show details")),
            Arg.Any<ChatScope>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithoutCompanyId_PassesMessageDirectly()
    {
        // Arrange
        var command = new ChatCommand("Hello world", null, Guid.NewGuid(), ChatScope.Admin);
        var expectedResponse = new ChatResponse { Response = "Hi", ConversationId = Guid.NewGuid() };

        _chatService.RunChatAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ChatScope>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _chatService.Received(1).RunChatAsync(
            Arg.Any<string>(),
            "Hello world",
            Arg.Any<ChatScope>(),
            Arg.Any<CancellationToken>());
    }
}
