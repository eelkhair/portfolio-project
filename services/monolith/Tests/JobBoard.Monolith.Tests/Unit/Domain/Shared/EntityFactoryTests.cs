using Shouldly;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;

namespace JobBoard.Monolith.Tests.Unit.Domain.Shared;

[Trait("Category", "Unit")]
public class EntityFactoryTests
{
    private class TestEntity : BaseAuditableEntity;

    [Fact]
    public void ApplyAudit_WithCreatedAt_ShouldSetBothTimestamps()
    {
        var entity = new TestEntity();
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        EntityFactory.ApplyAudit(entity, createdAt, null);

        entity.CreatedAt.ShouldBe(createdAt);
        entity.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void ApplyAudit_WithCreatedBy_ShouldSetBothUsers()
    {
        var entity = new TestEntity();

        EntityFactory.ApplyAudit(entity, null, "admin@test.com");

        entity.CreatedBy.ShouldBe("admin@test.com");
        entity.UpdatedBy.ShouldBe("admin@test.com");
    }

    [Fact]
    public void ApplyAudit_WithNullValues_ShouldNotModifyEntity()
    {
        var entity = new TestEntity();

        EntityFactory.ApplyAudit(entity, null, null);

        entity.CreatedAt.ShouldBe(default);
        entity.CreatedBy.ShouldBe(string.Empty);
        entity.UpdatedAt.ShouldBe(default);
        entity.UpdatedBy.ShouldBe(string.Empty);
    }

    [Fact]
    public void ApplyAudit_WithEmptyCreatedBy_ShouldNotModifyUsers()
    {
        var entity = new TestEntity();

        EntityFactory.ApplyAudit(entity, null, "");

        entity.CreatedBy.ShouldBe(string.Empty);
        entity.UpdatedBy.ShouldBe(string.Empty);
    }

    [Fact]
    public void ApplyAudit_ShouldReturnSameEntity()
    {
        var entity = new TestEntity();

        var result = EntityFactory.ApplyAudit(entity, DateTime.UtcNow, "user");

        result.ShouldBeSameAs(entity);
    }
}
