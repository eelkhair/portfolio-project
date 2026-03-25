using System.Diagnostics;
using System.Net;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using UserApi.Application.Commands;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;
using UserAPI.Contracts.Models.Events;

namespace UserApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class KeycloakCommandServiceTests
{
    private readonly IKeycloakFactory _factory = Substitute.For<IKeycloakFactory>();
    private readonly IKeycloakResource _resource = Substitute.For<IKeycloakResource>();
    private readonly ILogger<KeycloakCommandService> _logger = Substitute.For<ILogger<KeycloakCommandService>>();
    private readonly ActivitySource _activitySource = new("UserApi.Tests");
    private readonly KeycloakCommandService _sut;

    private readonly ProvisionUserEvent _event = new()
    {
        CompanyName = "Acme Inc",
        FirstName = "Jane",
        LastName = "Smith",
        Email = "jane@acme.com",
        CompanyUId = Guid.NewGuid(),
        CompanyEmail = "admin@acme.com"
    };

    public KeycloakCommandServiceTests()
    {
        _factory.GetKeycloakResourceAsync(Arg.Any<CancellationToken>()).Returns(_resource);
        _sut = new KeycloakCommandService(_activitySource, _factory, _logger);
    }

    [Fact]
    public async Task ProvisionUserAsync_FullFlow_ShouldExecuteAllSteps()
    {
        // Arrange
        var groupId = Guid.NewGuid().ToString();
        var companyAdminsGroupId = Guid.NewGuid().ToString();
        var recruitersGroupId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        SetupCreateGroup(groupId);
        SetupCreateSubGroup(groupId, "CompanyAdmins", companyAdminsGroupId);
        SetupCreateSubGroup(groupId, "Recruiters", recruitersGroupId);
        SetupCreateUser(userId, HttpStatusCode.Created);
        SetupAddUserToGroup(userId, companyAdminsGroupId);
        SetupSendVerifyEmail(userId);

        // Act
        var (user, group) = await _sut.ProvisionUserAsync(_event, CancellationToken.None);

        // Assert
        group.Id.ShouldBe(groupId);
        user.Id.ShouldBe(userId);

        await _resource.Received(1).CreateGroupAsync(_event.CompanyUId, _event.CompanyName, Arg.Any<CancellationToken>());
        await _resource.Received(1).CreateSubGroupAsync(groupId, "CompanyAdmins", Arg.Any<CancellationToken>());
        await _resource.Received(1).CreateSubGroupAsync(groupId, "Recruiters", Arg.Any<CancellationToken>());
        await _resource.Received(1).CreateUserAsync(_event.Email, _event.FirstName, _event.LastName,
            Arg.Any<Dictionary<string, List<string>>>(), Arg.Any<CancellationToken>());
        await _resource.Received(1).AddUserToGroupAsync(userId, companyAdminsGroupId, Arg.Any<CancellationToken>());
        await _resource.Received(1).SendVerifyEmailAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionUserAsync_WhenUserAlreadyExists_ShouldSkipVerificationEmail()
    {
        // Arrange - user creation returns OK (existing) not Created (new)
        var groupId = Guid.NewGuid().ToString();
        var companyAdminsGroupId = Guid.NewGuid().ToString();
        var recruitersGroupId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        SetupCreateGroup(groupId);
        SetupCreateSubGroup(groupId, "CompanyAdmins", companyAdminsGroupId);
        SetupCreateSubGroup(groupId, "Recruiters", recruitersGroupId);
        SetupCreateUser(userId, HttpStatusCode.OK);
        SetupAddUserToGroup(userId, companyAdminsGroupId);

        // Act
        await _sut.ProvisionUserAsync(_event, CancellationToken.None);

        // Assert - verification email should NOT be sent for existing users
        await _resource.DidNotReceive().SendVerifyEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionUserAsync_WhenVerificationEmailFails_ShouldNotThrow()
    {
        // Arrange
        var groupId = Guid.NewGuid().ToString();
        var companyAdminsGroupId = Guid.NewGuid().ToString();
        var recruitersGroupId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        SetupCreateGroup(groupId);
        SetupCreateSubGroup(groupId, "CompanyAdmins", companyAdminsGroupId);
        SetupCreateSubGroup(groupId, "Recruiters", recruitersGroupId);
        SetupCreateUser(userId, HttpStatusCode.Created);
        SetupAddUserToGroup(userId, companyAdminsGroupId);

        _resource.SendVerifyEmailAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<bool>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError { Message = "SMTP down" }
            });

        // Act & Assert - should not throw
        var (user, group) = await _sut.ProvisionUserAsync(_event, CancellationToken.None);
        user.Id.ShouldBe(userId);
        group.Id.ShouldBe(groupId);
    }

    [Fact]
    public async Task ProvisionUserAsync_WhenCreateGroupFails_ShouldThrow()
    {
        // Arrange
        _resource.CreateGroupAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<KeycloakGroup>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError { Message = "Keycloak unavailable" }
            });

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => _sut.ProvisionUserAsync(_event, CancellationToken.None));
        ex.Message.ShouldContain("Error creating company group");
        ex.Message.ShouldContain("Keycloak unavailable");
    }

    [Fact]
    public async Task ProvisionUserAsync_WhenCreateSubGroupFails_ShouldThrow()
    {
        // Arrange
        var groupId = Guid.NewGuid().ToString();
        SetupCreateGroup(groupId);

        _resource.CreateSubGroupAsync(groupId, "CompanyAdmins", Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<KeycloakGroup>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError { Message = "Sub-group creation failed" }
            });

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => _sut.ProvisionUserAsync(_event, CancellationToken.None));
        ex.Message.ShouldContain("Error creating CompanyAdmins sub-group");
    }

    [Fact]
    public async Task ProvisionUserAsync_WhenCreateUserFails_ShouldThrow()
    {
        // Arrange
        var groupId = Guid.NewGuid().ToString();
        var companyAdminsGroupId = Guid.NewGuid().ToString();
        var recruitersGroupId = Guid.NewGuid().ToString();

        SetupCreateGroup(groupId);
        SetupCreateSubGroup(groupId, "CompanyAdmins", companyAdminsGroupId);
        SetupCreateSubGroup(groupId, "Recruiters", recruitersGroupId);

        _resource.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Dictionary<string, List<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<KeycloakUser>
            {
                Success = false,
                StatusCode = HttpStatusCode.Conflict,
                Exceptions = new ApiError { Message = "User email conflict" }
            });

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => _sut.ProvisionUserAsync(_event, CancellationToken.None));
        ex.Message.ShouldContain("Error creating user");
    }

    [Fact]
    public async Task ProvisionUserAsync_WhenAddUserToGroupFails_ShouldThrow()
    {
        // Arrange
        var groupId = Guid.NewGuid().ToString();
        var companyAdminsGroupId = Guid.NewGuid().ToString();
        var recruitersGroupId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        SetupCreateGroup(groupId);
        SetupCreateSubGroup(groupId, "CompanyAdmins", companyAdminsGroupId);
        SetupCreateSubGroup(groupId, "Recruiters", recruitersGroupId);
        SetupCreateUser(userId, HttpStatusCode.Created);

        _resource.AddUserToGroupAsync(userId, companyAdminsGroupId, Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<bool>
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Exceptions = new ApiError { Message = "Group not found" }
            });

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => _sut.ProvisionUserAsync(_event, CancellationToken.None));
        ex.Message.ShouldContain("Error adding user to CompanyAdmins group");
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldPassCompanyNameAsAttribute()
    {
        // Arrange
        var groupId = Guid.NewGuid().ToString();
        var companyAdminsGroupId = Guid.NewGuid().ToString();
        var recruitersGroupId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        SetupCreateGroup(groupId);
        SetupCreateSubGroup(groupId, "CompanyAdmins", companyAdminsGroupId);
        SetupCreateSubGroup(groupId, "Recruiters", recruitersGroupId);
        SetupCreateUser(userId, HttpStatusCode.Created);
        SetupAddUserToGroup(userId, companyAdminsGroupId);
        SetupSendVerifyEmail(userId);

        // Act
        await _sut.ProvisionUserAsync(_event, CancellationToken.None);

        // Assert - verify companyName attribute was passed
        await _resource.Received(1).CreateUserAsync(
            _event.Email,
            _event.FirstName,
            _event.LastName,
            Arg.Is<Dictionary<string, List<string>>>(d =>
                d.ContainsKey("companyName") && d["companyName"].Contains(_event.CompanyName)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionUserAsync_WhenErrorHasNestedErrors_ShouldExtractFirstError()
    {
        // Arrange - error with Errors dictionary but no Message
        _resource.CreateGroupAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<KeycloakGroup>
            {
                Success = false,
                StatusCode = HttpStatusCode.BadRequest,
                Exceptions = new ApiError
                {
                    Message = null,
                    Errors = new Dictionary<string, string[]>
                    {
                        ["field1"] = ["Validation error detail"]
                    }
                }
            });

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => _sut.ProvisionUserAsync(_event, CancellationToken.None));
        ex.Message.ShouldContain("Validation error detail");
    }

    #region Setup Helpers

    private void SetupCreateGroup(string groupId)
    {
        _resource.CreateGroupAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<KeycloakGroup>
            {
                Data = new KeycloakGroup { Id = groupId, Name = _event.CompanyUId.ToString() },
                Success = true,
                StatusCode = HttpStatusCode.Created
            });
    }

    private void SetupCreateSubGroup(string parentId, string name, string subGroupId)
    {
        _resource.CreateSubGroupAsync(parentId, name, Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<KeycloakGroup>
            {
                Data = new KeycloakGroup { Id = subGroupId, Name = name },
                Success = true,
                StatusCode = HttpStatusCode.Created
            });
    }

    private void SetupCreateUser(string userId, HttpStatusCode statusCode)
    {
        _resource.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Dictionary<string, List<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<KeycloakUser>
            {
                Data = new KeycloakUser { Id = userId, Email = _event.Email },
                Success = true,
                StatusCode = statusCode
            });
    }

    private void SetupAddUserToGroup(string userId, string groupId)
    {
        _resource.AddUserToGroupAsync(userId, groupId, Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<bool>
            {
                Data = true,
                Success = true,
                StatusCode = HttpStatusCode.NoContent
            });
    }

    private void SetupSendVerifyEmail(string userId)
    {
        _resource.SendVerifyEmailAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new ApiResponse<bool>
            {
                Data = true,
                Success = true,
                StatusCode = HttpStatusCode.NoContent
            });
    }

    #endregion
}
