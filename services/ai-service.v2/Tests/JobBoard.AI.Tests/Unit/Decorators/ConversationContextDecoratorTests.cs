using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Infrastructure.Decorators;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Decorators;

[Trait("Category", "Unit")]
public class ConversationContextDecoratorTests
{
    private readonly IHandler<ChatCommand, ChatResponse> _innerHandler =
        Substitute.For<IHandler<ChatCommand, ChatResponse>>();

    private readonly IConversationContext _conversationContext = Substitute.For<IConversationContext>();
    private readonly ILogger<ConversationContextDecorator<ChatCommand, ChatResponse>> _logger;
    private readonly ConversationContextDecorator<ChatCommand, ChatResponse> _sut;

    public ConversationContextDecoratorTests()
    {
        _logger = Substitute.For<ILogger<ConversationContextDecorator<ChatCommand, ChatResponse>>>();
        _sut = new ConversationContextDecorator<ChatCommand, ChatResponse>(
            _innerHandler, _conversationContext, _logger);
    }

    [Fact]
    public async Task HandleAsync_WithExistingConversationId_UsesExistingId()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var command = new ChatCommand("Hello", null, existingId, ChatScope.Admin);
        var expectedResponse = new ChatResponse { Response = "Hi" };
        _innerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns(expectedResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
        _conversationContext.ConversationId = existingId;
    }

    [Fact]
    public async Task HandleAsync_WithNullConversationId_GeneratesNewId()
    {
        // Arrange
        var command = new ChatCommand("Hello", null, null, ChatScope.Admin);
        _innerHandler.HandleAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ChatResponse { Response = "Hi" });

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — a new Guid should have been assigned
        _conversationContext.Received().ConversationId = Arg.Is<Guid?>(g => g.HasValue && g.Value != Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_CallsInnerHandler()
    {
        // Arrange
        var command = new ChatCommand("Test", null, Guid.NewGuid(), ChatScope.Public);
        _innerHandler.HandleAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ChatResponse { Response = "Reply" });

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _innerHandler.Received(1).HandleAsync(command, Arg.Any<CancellationToken>());
    }
}
