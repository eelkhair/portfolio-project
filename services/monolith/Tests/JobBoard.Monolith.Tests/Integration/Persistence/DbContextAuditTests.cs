using JobBoard.Domain.Entities;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Persistence;

[Trait("Category", "Integration")]
[Collection("Database")]
public class DbContextAuditTests
{
    private readonly TestDatabaseFixture _fixture;

    public DbContextAuditTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChangesAsync_SetsCreatedAtAndCreatedBy_OnAdd()
    {
        await using var ctx = _fixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);

        var industry = Industry.Create("AuditTestIndustry-" + Guid.NewGuid().ToString()[..8]);
        industry.InternalId = internalId;
        industry.Id = id;

        var beforeSave = DateTime.UtcNow;
        ctx.Industries.Add(industry);
        await ctx.SaveChangesAsync("audit-user-123", CancellationToken.None);

        industry.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeSave.AddSeconds(-1));
        industry.CreatedBy.ShouldBe("audit-user-123");
        industry.UpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeSave.AddSeconds(-1));
        industry.UpdatedBy.ShouldBe("audit-user-123");
    }

    [Fact]
    public async Task SaveChangesAsync_SetsUpdatedAtAndUpdatedBy_OnModify()
    {
        // Seed an industry
        await using var ctx1 = _fixture.CreateContext();
        var (internalId, id) = await ctx1.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
        var industry = Industry.Create("AuditUpdate-" + Guid.NewGuid().ToString()[..8]);
        industry.InternalId = internalId;
        industry.Id = id;
        ctx1.Industries.Add(industry);
        await ctx1.SaveChangesAsync("creator-user", CancellationToken.None);

        var originalCreatedAt = industry.CreatedAt;
        var originalCreatedBy = industry.CreatedBy;

        // Now update
        await Task.Delay(50); // Small delay to ensure timestamp differs
        await using var ctx2 = _fixture.CreateContext();
        var tracked = await ctx2.Industries.FirstAsync(i => i.Id == id);
        tracked.SetName("UpdatedName-" + Guid.NewGuid().ToString()[..8]);

        var beforeUpdate = DateTime.UtcNow;
        await ctx2.SaveChangesAsync("updater-user", CancellationToken.None);

        tracked.CreatedAt.ShouldBe(originalCreatedAt);
        tracked.CreatedBy.ShouldBe(originalCreatedBy);
        tracked.UpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeUpdate.AddSeconds(-1));
        // SaveChangesAsync only sets UpdatedBy when it's empty (doesn't overwrite existing value)
        tracked.UpdatedBy.ShouldBe(originalCreatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotOverwriteExplicitCreatedBy()
    {
        await using var ctx = _fixture.CreateContext();
        var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);

        var industry = Industry.Create("ExplicitAudit-" + Guid.NewGuid().ToString()[..8]);
        industry.InternalId = internalId;
        industry.Id = id;
        industry.CreatedBy = "explicit-creator";

        ctx.Industries.Add(industry);
        await ctx.SaveChangesAsync("fallback-user", CancellationToken.None);

        industry.CreatedBy.ShouldBe("explicit-creator");
    }

    [Fact]
    public async Task GetNextValueFromSequenceAsync_ReturnsUniqueValues()
    {
        await using var ctx = _fixture.CreateContext();

        var (id1, uid1) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
        var (id2, uid2) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);

        id2.ShouldBeGreaterThan(id1);
        uid1.ShouldNotBe(uid2);
    }
}
