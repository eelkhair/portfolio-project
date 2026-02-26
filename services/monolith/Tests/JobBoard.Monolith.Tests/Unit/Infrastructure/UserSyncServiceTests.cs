using JobBoard.Application.Infrastructure.UserSync;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Domain.Entities.Users;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class UserSyncServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUserAccessor _userAccessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserSyncService _sut;

    public UserSyncServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userAccessor = Substitute.For<IUserAccessor>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new UserSyncService(_userRepository, _userAccessor, _unitOfWork);
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenUserNotFound_ShouldCreateNewUser()
    {
        const string userId = "ext-user-1";
        _userRepository.FindUserByExternalIdOrIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _unitOfWork.GetNextValueFromSequenceAsync(typeof(User), Arg.Any<CancellationToken>())
            .Returns((1, Guid.NewGuid()));
        _userAccessor.FirstName.Returns("John");
        _userAccessor.LastName.Returns("Doe");
        _userAccessor.Email.Returns("john@test.com");

        await _sut.EnsureUserExistsAsync(userId, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u =>
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.Email == "john@test.com" &&
                u.ExternalId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenUserExistsAndClaimsDiffer_ShouldUpdateUser()
    {
        const string userId = "ext-user-2";
        var existingUser = User.Create("Old", "Name", "old@test.com", userId,
            Guid.NewGuid(), 1, DateTime.UtcNow, userId);
        _userRepository.FindUserByExternalIdOrIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAccessor.FirstName.Returns("New");
        _userAccessor.LastName.Returns("Updated");
        _userAccessor.Email.Returns("new@test.com");

        await _sut.EnsureUserExistsAsync(userId, CancellationToken.None);

        existingUser.FirstName.ShouldBe("New");
        existingUser.LastName.ShouldBe("Updated");
        existingUser.Email.ShouldBe("new@test.com");
        existingUser.UpdatedBy.ShouldBe(userId);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenUserExistsAndClaimsMatch_ShouldNotMutate()
    {
        const string userId = "ext-user-3";
        var existingUser = User.Create("John", "Doe", "john@test.com", userId,
            Guid.NewGuid(), 1, DateTime.UtcNow, userId);
        var originalUpdatedAt = existingUser.UpdatedAt;
        _userRepository.FindUserByExternalIdOrIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAccessor.FirstName.Returns("John");
        _userAccessor.LastName.Returns("Doe");
        _userAccessor.Email.Returns("john@test.com");

        await _sut.EnsureUserExistsAsync(userId, CancellationToken.None);

        existingUser.UpdatedAt.ShouldBe(originalUpdatedAt);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureUserExistsAsync_AlwaysCallsSaveChanges()
    {
        const string userId = "ext-user-4";
        _userRepository.FindUserByExternalIdOrIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _unitOfWork.GetNextValueFromSequenceAsync(typeof(User), Arg.Any<CancellationToken>())
            .Returns((1, Guid.NewGuid()));
        _userAccessor.FirstName.Returns("Test");
        _userAccessor.LastName.Returns("User");
        _userAccessor.Email.Returns("test@user.com");

        await _sut.EnsureUserExistsAsync(userId, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenExistingUserAndClaimsMatch_StillCallsSaveChanges()
    {
        const string userId = "ext-user-5";
        var existingUser = User.Create("Same", "Name", "same@test.com", userId,
            Guid.NewGuid(), 1, DateTime.UtcNow, userId);
        _userRepository.FindUserByExternalIdOrIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAccessor.FirstName.Returns("Same");
        _userAccessor.LastName.Returns("Name");
        _userAccessor.Email.Returns("same@test.com");

        await _sut.EnsureUserExistsAsync(userId, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenClaimsPartiallyNull_ShouldNotUpdate()
    {
        const string userId = "ext-user-6";
        var existingUser = User.Create("John", "Doe", "john@test.com", userId,
            Guid.NewGuid(), 1, DateTime.UtcNow, userId);
        var originalUpdatedAt = existingUser.UpdatedAt;
        _userRepository.FindUserByExternalIdOrIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAccessor.FirstName.Returns((string?)null);
        _userAccessor.LastName.Returns("Doe");
        _userAccessor.Email.Returns("john@test.com");

        await _sut.EnsureUserExistsAsync(userId, CancellationToken.None);

        // Should not update because FirstName is null
        existingUser.UpdatedAt.ShouldBe(originalUpdatedAt);
    }
}
