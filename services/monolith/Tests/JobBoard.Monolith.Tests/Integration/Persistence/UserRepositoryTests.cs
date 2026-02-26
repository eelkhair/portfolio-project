using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.Infrastructure.Persistence.Repositories;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Persistence;

[Trait("Category", "Integration")]
[Collection("Database")]
public class UserRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;

    public UserRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<User> SeedUserAsync(string? externalId = null)
    {
        await using var ctx = _fixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(User), CancellationToken.None);
        var suffix = Guid.NewGuid().ToString()[..8];
        var user = User.Create(
            firstName: "Test",
            lastName: "User",
            email: $"user-{suffix}@test.com",
            externalId: externalId ?? $"ext-{suffix}",
            id: id,
            internalId: internalId,
            createdAt: DateTime.UtcNow,
            createdBy: "seed");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return user;
    }

    private async Task<(Industry industry, int industryInternalId)> SeedIndustryAsync()
    {
        await using var ctx = _fixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
        var industry = Industry.Create("Ind-" + Guid.NewGuid().ToString()[..8]);
        industry.InternalId = internalId;
        industry.Id = id;
        industry.CreatedAt = DateTime.UtcNow;
        industry.CreatedBy = "seed";
        industry.UpdatedAt = DateTime.UtcNow;
        industry.UpdatedBy = "seed";
        ctx.Industries.Add(industry);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return (industry, internalId);
    }

    [Fact]
    public async Task AddAsync_And_FindUserByUIdAsync_RoundTrip()
    {
        var seeded = await SeedUserAsync();

        await using var ctx = _fixture.CreateContext();
        var repo = new UserRepository(ctx);

        var retrieved = await repo.FindUserByUIdAsync(seeded.Id, CancellationToken.None);

        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(seeded.Id);
        retrieved.FirstName.ShouldBe("Test");
        retrieved.LastName.ShouldBe("User");
    }

    [Fact]
    public async Task FindUserByIdAsync_ReturnsNull_WhenUserNotFound()
    {
        await using var ctx = _fixture.CreateContext();
        var repo = new UserRepository(ctx);

        var retrieved = await repo.FindUserByIdAsync("nonexistent-id", CancellationToken.None);

        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task FindUserByExternalIdOrIdAsync_FindsByExternalId()
    {
        var externalId = $"auth0|{Guid.NewGuid():N}";
        var seeded = await SeedUserAsync(externalId);

        await using var ctx = _fixture.CreateContext();
        var repo = new UserRepository(ctx);

        var retrieved = await repo.FindUserByExternalIdOrIdAsync(externalId, CancellationToken.None);

        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(seeded.Id);
        retrieved.ExternalId.ShouldBe(externalId);
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsTrue_WhenEmailExists()
    {
        var seeded = await SeedUserAsync();

        await using var ctx = _fixture.CreateContext();
        var repo = new UserRepository(ctx);

        var exists = await repo.EmailExistsAsync(seeded.Email, CancellationToken.None);
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsFalse_WhenEmailDoesNotExist()
    {
        await using var ctx = _fixture.CreateContext();
        var repo = new UserRepository(ctx);

        var exists = await repo.EmailExistsAsync($"nonexistent-{Guid.NewGuid()}@test.com", CancellationToken.None);
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task AddCompanyUser_CreatesUserCompanyLink()
    {
        var seeded = await SeedUserAsync();
        var (_, industryInternalId) = await SeedIndustryAsync();

        // Seed a company
        await using var ctx1 = _fixture.CreateContext();
        var (companyInternalId, companyId) = await ctx1.GetNextValueFromSequenceAsync(typeof(Company), CancellationToken.None);
        var company = Company.Create(new CompanyInput(
            InternalId: companyInternalId,
            Id: companyId,
            Name: $"UCTestCorp-{Guid.NewGuid().ToString()[..8]}",
            Email: $"uc-{Guid.NewGuid().ToString()[..8]}@test.com",
            Status: "Provisioning",
            IndustryId: industryInternalId,
            CreatedAt: DateTime.UtcNow,
            CreatedBy: "seed"));
        ctx1.Companies.Add(company);
        await ctx1.SaveChangesAsync("seed", CancellationToken.None);

        // Create UserCompany link
        await using var ctx2 = _fixture.CreateContext();
        var (ucInternalId, ucId) = await ctx2.GetNextValueFromSequenceAsync(typeof(UserCompany), CancellationToken.None);
        var userCompany = UserCompany.Create(
            seeded.InternalId, companyInternalId, ucInternalId, ucId, DateTime.UtcNow, "seed");

        var repo = new UserRepository(ctx2);
        await repo.AddCompanyUser(userCompany, CancellationToken.None);
        await ctx2.SaveChangesAsync("seed", CancellationToken.None);

        // Verify
        await using var ctx3 = _fixture.CreateContext();
        var link = await ctx3.UserCompanies.FindAsync(ucInternalId);
        link.ShouldNotBeNull();
        link.UserId.ShouldBe(seeded.InternalId);
        link.CompanyId.ShouldBe(companyInternalId);
    }
}
