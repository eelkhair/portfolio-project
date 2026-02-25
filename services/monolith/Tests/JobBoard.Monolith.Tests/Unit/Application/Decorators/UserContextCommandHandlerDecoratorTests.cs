using JobBoard.Application.Infrastructure.Decorators;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Users;
using NSubstitute;
using Shouldly;
using System.Diagnostics;
using JobBoard.Monolith.Tests.Unit.Application.Helpers;

namespace JobBoard.Monolith.Tests.Unit.Application.Decorators;

[Trait("Category", "Unit")]
public class UserContextCommandHandlerDecoratorTests
{
    private readonly IHandler<TestCommand, string> _innerHandler;
    private readonly IActivityFactory _activityFactory;
    private readonly IUserAccessor _userAccessor;
    private readonly IUserSyncService _userSyncService;
    private readonly UserContextCommandHandlerDecorator<TestCommand, string> _sut;

    public UserContextCommandHandlerDecoratorTests()
    {
        _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
        _activityFactory = Substitute.For<IActivityFactory>();
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);
        _userAccessor = Substitute.For<IUserAccessor>();
        _userSyncService = Substitute.For<IUserSyncService>();
        _sut = new UserContextCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, _userAccessor, _userSyncService);
    }

    [Fact]
    public async Task HandleAsync_WithAuthenticatedUser_ShouldSetUserIdAndCallInnerHandler()
    {
        const string userId = "auth0|user123";
        _userAccessor.UserId.Returns(userId);
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns("ok");

        var result = await _sut.HandleAsync(request, CancellationToken.None);

        result.ShouldBe("ok");
        request.UserId.ShouldBe(userId);
        await _innerHandler.Received(1).HandleAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithAuthenticatedUser_ShouldCallEnsureUserExists()
    {
        const string userId = "auth0|user123";
        _userAccessor.UserId.Returns(userId);
        _innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>()).Returns("ok");

        await _sut.HandleAsync(new TestCommand(), CancellationToken.None);

        await _userSyncService.Received(1).EnsureUserExistsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleAsync_WhenUserIdNullOrEmpty_ShouldThrowUnauthorizedAccessException(string? userId)
    {
        _userAccessor.UserId.Returns(userId);
        var request = new TestCommand { Name = "Test" };

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _sut.HandleAsync(request, CancellationToken.None));

        await _innerHandler.DidNotReceive().HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
    }
}
