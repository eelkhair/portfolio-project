using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Infrastructure.Decorators;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Decorators;

// System command for testing ISystemCommand bypass
public class TestSystemCommand : BaseCommand<string>, ISystemCommand
{
    public string Input { get; set; } = string.Empty;
}

[Trait("Category", "Unit")]
public class UserContextCommandHandlerDecoratorTests
{
    private readonly IHandler<TestCommand, string> _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
    private readonly IHandler<TestSystemCommand, string> _systemInnerHandler = Substitute.For<IHandler<TestSystemCommand, string>>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly IUserAccessor _userAccessor = Substitute.For<IUserAccessor>();

    public UserContextCommandHandlerDecoratorTests()
    {
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_SetsUserIdAndCallsInner()
    {
        // Arrange
        _userAccessor.UserId.Returns("user-123");
        var decorator = new UserContextCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, _userAccessor);

        var command = new TestCommand { Input = "test" };
        _innerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns("result");

        // Act
        var result = await decorator.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe("result");
        command.UserId.ShouldBe("user-123");
        await _innerHandler.Received(1).HandleAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnauthenticatedUser_ThrowsUnauthorized()
    {
        // Arrange
        _userAccessor.UserId.Returns((string?)null);
        var decorator = new UserContextCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, _userAccessor);

        var command = new TestCommand();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => decorator.HandleAsync(command, CancellationToken.None));

        await _innerHandler.DidNotReceive().HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyUserId_ThrowsUnauthorized()
    {
        // Arrange
        _userAccessor.UserId.Returns("");
        var decorator = new UserContextCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, _userAccessor);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => decorator.HandleAsync(new TestCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_SystemCommand_WithNullUser_StillCallsInner()
    {
        // Arrange
        _userAccessor.UserId.Returns((string?)null);
        var decorator = new UserContextCommandHandlerDecorator<TestSystemCommand, string>(
            _systemInnerHandler, _activityFactory, _userAccessor);

        var command = new TestSystemCommand();
        _systemInnerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns("system-result");

        // Act
        var result = await decorator.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe("system-result");
        command.UserId.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task HandleAsync_SystemCommand_WithEmptyUser_StillCallsInner()
    {
        // Arrange
        _userAccessor.UserId.Returns("");
        var decorator = new UserContextCommandHandlerDecorator<TestSystemCommand, string>(
            _systemInnerHandler, _activityFactory, _userAccessor);

        var command = new TestSystemCommand();
        _systemInnerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns("ok");

        // Act
        var result = await decorator.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe("ok");
    }
}
