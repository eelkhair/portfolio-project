using JobBoard.Application.Actions.Users.Get;
using JobBoard.Application.Interfaces;
using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class GetUsersQueryHandlerTests
{
    private readonly GetUsersQueryHandler _sut;
    private readonly TestUserDbContext _dbContext;

    public GetUsersQueryHandlerTests()
    {
        _dbContext = new TestUserDbContext();

        var context = Substitute.For<IJobBoardQueryDbContext, ITransactionDbContext>();
        ((ITransactionDbContext)context).ChangeTracker.Returns(_dbContext.ChangeTracker);
        context.Users.Returns(_dbContext.Users);

        _sut = new GetUsersQueryHandler(
            context,
            Substitute.For<ILogger<GetUsersQueryHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldProjectAllUserFields()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var user = User.Create("John", "Doe", "john@test.com", "ext-123",
            userId, 1, now, "admin");
        user.UpdatedAt = now;
        user.UpdatedBy = "admin";
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetUsersQuery(), CancellationToken.None);
        var dto = result.First();

        dto.FirstName.ShouldBe("John");
        dto.LastName.ShouldBe("Doe");
        dto.Email.ShouldBe("john@test.com");
        dto.ExternalId.ShouldBe("ext-123");
        dto.Id.ShouldBe(userId);
        dto.CreatedAt.ShouldBe(now, TimeSpan.FromSeconds(1));
        dto.CreatedBy.ShouldBe("admin");
        dto.UpdatedAt.ShouldBe(now, TimeSpan.FromSeconds(1));
        dto.UpdatedBy.ShouldBe("admin");
    }

    [Fact]
    public async Task HandleAsync_WithNoUsers_ShouldReturnEmptyQueryable()
    {
        var result = await _sut.HandleAsync(new GetUsersQuery(), CancellationToken.None);

        result.ToList().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithMultipleUsers_ShouldReturnAll()
    {
        _dbContext.Users.Add(User.Create("Alice", "Smith", "alice@test.com", "ext-1",
            Guid.NewGuid(), 1, DateTime.UtcNow, "seed"));
        _dbContext.Users.Add(User.Create("Bob", "Jones", "bob@test.com", "ext-2",
            Guid.NewGuid(), 2, DateTime.UtcNow, "seed"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetUsersQuery(), CancellationToken.None);

        result.ToList().Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleNullExternalId()
    {
        _dbContext.Users.Add(User.Create("Jane", "Doe", "jane@test.com", null,
            Guid.NewGuid(), 1, DateTime.UtcNow, "seed"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetUsersQuery(), CancellationToken.None);
        var dto = result.First();

        dto.ExternalId.ShouldBeNull();
    }
}

internal class TestUserDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase($"UserTests_{Guid.NewGuid()}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.InternalId);
    }
}
