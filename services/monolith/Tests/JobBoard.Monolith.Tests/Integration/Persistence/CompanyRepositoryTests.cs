using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Infrastructure.Persistence.Repositories;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Persistence;

[Trait("Category", "Integration")]
[Collection("Database")]
public class CompanyRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;

    public CompanyRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Industry industry, int industryInternalId, Guid industryId)> SeedIndustryAsync()
    {
        await using var ctx = _fixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
        var industry = Industry.Create("Tech-" + Guid.NewGuid().ToString()[..8]);
        industry.InternalId = internalId;
        industry.Id = id;
        industry.CreatedAt = DateTime.UtcNow;
        industry.CreatedBy = "seed";
        industry.UpdatedAt = DateTime.UtcNow;
        industry.UpdatedBy = "seed";
        ctx.Industries.Add(industry);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return (industry, internalId, id);
    }

    private async Task<Company> SeedCompanyAsync(int industryInternalId, string? nameSuffix = null)
    {
        await using var ctx = _fixture.CreateContext();
        var (companyInternalId, companyId) = await ctx.GetNextValueFromSequenceAsync(typeof(Company), CancellationToken.None);
        var suffix = nameSuffix ?? Guid.NewGuid().ToString()[..8];
        var company = Company.Create(new CompanyInput(
            InternalId: companyInternalId,
            Id: companyId,
            Name: $"TestCorp-{suffix}",
            Email: $"info-{suffix}@test.com",
            Status: "Provisioning",
            IndustryId: industryInternalId,
            Website: "https://test.com",
            CreatedAt: DateTime.UtcNow,
            CreatedBy: "seed"));
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync("seed", CancellationToken.None);
        return company;
    }

    [Fact]
    public async Task AddAsync_And_GetCompanyById_RoundTrip()
    {
        var (_, industryInternalId, _) = await SeedIndustryAsync();
        var seeded = await SeedCompanyAsync(industryInternalId);

        await using var ctx = _fixture.CreateContext();
        var repo = new CompanyRepository(ctx);

        var retrieved = await repo.GetCompanyById(seeded.Id, CancellationToken.None);

        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(seeded.Id);
        retrieved.Name.ShouldBe(seeded.Name);
        retrieved.Email.ShouldBe(seeded.Email);
    }

    [Fact]
    public async Task NameExistsAsync_ReturnsTrue_WhenCompanyExists()
    {
        var (_, industryInternalId, _) = await SeedIndustryAsync();
        var seeded = await SeedCompanyAsync(industryInternalId);

        await using var ctx = _fixture.CreateContext();
        var repo = new CompanyRepository(ctx);

        var exists = await repo.NameExistsAsync(seeded.Name, CancellationToken.None);
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_ReturnsFalse_WhenCompanyDoesNotExist()
    {
        await using var ctx = _fixture.CreateContext();
        var repo = new CompanyRepository(ctx);

        var exists = await repo.NameExistsAsync("NonExistentCompany-" + Guid.NewGuid(), CancellationToken.None);
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsTrue_WhenCompanyEmailExists()
    {
        var (_, industryInternalId, _) = await SeedIndustryAsync();
        var seeded = await SeedCompanyAsync(industryInternalId);

        await using var ctx = _fixture.CreateContext();
        var repo = new CompanyRepository(ctx);

        var exists = await repo.EmailExistsAsync(seeded.Email, CancellationToken.None);
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task IndustryExistsAsync_ReturnsTrue_WhenIndustryExists()
    {
        var (_, _, industryId) = await SeedIndustryAsync();

        await using var ctx = _fixture.CreateContext();
        var repo = new CompanyRepository(ctx);

        var exists = await repo.IndustryExistsAsync(industryId, CancellationToken.None);
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task IndustryExistsAsync_ReturnsFalse_WhenIndustryDoesNotExist()
    {
        await using var ctx = _fixture.CreateContext();
        var repo = new CompanyRepository(ctx);

        var exists = await repo.IndustryExistsAsync(Guid.NewGuid(), CancellationToken.None);
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetIndustryIdByUId_ReturnsInternalId()
    {
        var (_, industryInternalId, industryId) = await SeedIndustryAsync();

        await using var ctx = _fixture.CreateContext();
        var repo = new CompanyRepository(ctx);

        var result = await repo.GetIndustryIdByUId(industryId, CancellationToken.None);
        result.ShouldBe(industryInternalId);
    }
}
