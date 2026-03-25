using Microsoft.EntityFrameworkCore;
using Shouldly;
using UserApi.Application.Commands;
using UserApi.Infrastructure.Data;
using UserApi.Tests.Helpers;
using UserAPI.Contracts.Models.Requests;

namespace UserApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class CompanyCommandServiceTests : IAsyncLifetime
{
    private UserDbContext _context = null!;
    private CompanyCommandService _sut = null!;

    public Task InitializeAsync()
    {
        _context = TestDbContextFactory.Create();
        _sut = new CompanyCommandService(_context);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    #region CreateUser

    [Fact]
    public async Task CreateUser_ShouldPersistUser()
    {
        var request = new CreateUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            KeycloakId = "kc-123"
        };

        var id = await _sut.CreateUser(request, "admin-1", CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var saved = await _context.Users.FirstAsync();
        saved.FirstName.ShouldBe("Jane");
        saved.LastName.ShouldBe("Doe");
        saved.Email.ShouldBe("jane@test.com");
        saved.KeycloakUserId.ShouldBe("kc-123");
    }

    [Fact]
    public async Task CreateUser_WithUId_ShouldUseProvidedUId()
    {
        var uid = Guid.NewGuid();
        var request = new CreateUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            KeycloakId = "kc-123",
            UId = uid
        };

        await _sut.CreateUser(request, "admin-1", CancellationToken.None);

        var saved = await _context.Users.FirstAsync();
        saved.UId.ShouldBe(uid);
    }

    [Fact]
    public async Task CreateUser_WhenEmailExists_ShouldReturnExistingId()
    {
        // Arrange - seed an existing user
        var existing = TestDbContextFactory.CreateUser(email: "jane@test.com");
        _context.Users.Add(existing);
        await _context.SaveChangesAsync("seed", CancellationToken.None);

        var request = new CreateUserRequest
        {
            FirstName = "Different",
            LastName = "Name",
            Email = "jane@test.com",
            KeycloakId = "kc-new"
        };

        // Act
        var id = await _sut.CreateUser(request, "admin-1", CancellationToken.None);

        // Assert - should return the existing user's Id, not create a new one
        id.ShouldBe(existing.Id);
        (await _context.Users.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task CreateUser_ShouldSetAuditFields()
    {
        var request = new CreateUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            KeycloakId = "kc-123"
        };

        await _sut.CreateUser(request, "admin-user-id", CancellationToken.None);

        var saved = await _context.Users.FirstAsync();
        saved.CreatedBy.ShouldBe("admin-user-id");
        saved.UpdatedBy.ShouldBe("admin-user-id");
        saved.CreatedAt.ShouldNotBe(default);
        saved.UpdatedAt.ShouldNotBeNull();
    }

    #endregion

    #region CreateCompany

    [Fact]
    public async Task CreateCompany_ShouldPersistCompany()
    {
        var uid = Guid.NewGuid();
        var request = new CreateCompanyRequest
        {
            Name = "Acme Inc",
            KeycloakGroupId = "kc-group-456",
            UId = uid
        };

        var id = await _sut.CreateCompany(request, "admin-1", CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var saved = await _context.Companies.FirstAsync();
        saved.Name.ShouldBe("Acme Inc");
        saved.KeycloakGroupId.ShouldBe("kc-group-456");
        saved.UId.ShouldBe(uid);
    }

    [Fact]
    public async Task CreateCompany_ShouldSetAuditFields()
    {
        var request = new CreateCompanyRequest
        {
            Name = "Acme Inc",
            KeycloakGroupId = "kc-group-456",
            UId = Guid.NewGuid()
        };

        await _sut.CreateCompany(request, "admin-user-id", CancellationToken.None);

        var saved = await _context.Companies.FirstAsync();
        saved.CreatedBy.ShouldBe("admin-user-id");
        saved.UpdatedBy.ShouldBe("admin-user-id");
        saved.CreatedAt.ShouldNotBe(default);
    }

    #endregion

    #region AddUserToCompany

    [Fact]
    public async Task AddUserToCompany_ShouldPersistRelationship()
    {
        // Arrange
        var user = TestDbContextFactory.CreateUser();
        var company = TestDbContextFactory.CreateCompany();
        _context.Users.Add(user);
        _context.Companies.Add(company);
        await _context.SaveChangesAsync("seed", CancellationToken.None);

        // Act
        await _sut.AddUserToCompany(user.Id, company.Id, "admin-1", null, CancellationToken.None);

        // Assert
        var saved = await _context.UserCompanies.FirstAsync();
        saved.UserId.ShouldBe(user.Id);
        saved.CompanyId.ShouldBe(company.Id);
    }

    [Fact]
    public async Task AddUserToCompany_WithUId_ShouldUseProvidedUId()
    {
        // Arrange
        var user = TestDbContextFactory.CreateUser();
        var company = TestDbContextFactory.CreateCompany();
        _context.Users.Add(user);
        _context.Companies.Add(company);
        await _context.SaveChangesAsync("seed", CancellationToken.None);

        var ucUId = Guid.NewGuid();

        // Act
        await _sut.AddUserToCompany(user.Id, company.Id, "admin-1", ucUId, CancellationToken.None);

        // Assert
        var saved = await _context.UserCompanies.FirstAsync();
        saved.UId.ShouldBe(ucUId);
    }

    [Fact]
    public async Task AddUserToCompany_ShouldSetAuditFields()
    {
        // Arrange
        var user = TestDbContextFactory.CreateUser();
        var company = TestDbContextFactory.CreateCompany();
        _context.Users.Add(user);
        _context.Companies.Add(company);
        await _context.SaveChangesAsync("seed", CancellationToken.None);

        // Act
        await _sut.AddUserToCompany(user.Id, company.Id, "admin-user-id", null, CancellationToken.None);

        // Assert
        var saved = await _context.UserCompanies.FirstAsync();
        saved.CreatedBy.ShouldBe("admin-user-id");
        saved.UpdatedBy.ShouldBe("admin-user-id");
    }

    #endregion
}
