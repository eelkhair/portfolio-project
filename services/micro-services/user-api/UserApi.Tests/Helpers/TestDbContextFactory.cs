using Microsoft.EntityFrameworkCore;
using UserApi.Infrastructure.Data;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Tests.Helpers;

public static class TestDbContextFactory
{
    private static readonly DateTime Now = DateTime.UtcNow;

    public static UserDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new UserDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static User CreateUser(
        string email = "john@test.com",
        string firstName = "John",
        string lastName = "Doe",
        string? keycloakUserId = null,
        Guid? uid = null) => new()
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            KeycloakUserId = keycloakUserId ?? "kc-user-123",
            UId = uid ?? Guid.NewGuid(),
            CreatedAt = Now,
            UpdatedAt = Now,
            CreatedBy = "test",
            UpdatedBy = "test"
        };

    public static Company CreateCompany(
        string name = "Test Corp",
        string keycloakGroupId = "kc-group-123",
        Guid? uid = null) => new()
        {
            Name = name,
            KeycloakGroupId = keycloakGroupId,
            UId = uid ?? Guid.NewGuid(),
            CreatedAt = Now,
            UpdatedAt = Now,
            CreatedBy = "test",
            UpdatedBy = "test"
        };
}
