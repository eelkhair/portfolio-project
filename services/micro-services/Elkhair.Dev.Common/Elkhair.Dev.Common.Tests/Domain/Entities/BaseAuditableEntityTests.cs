using Elkhair.Dev.Common.Domain.Constants;
using Elkhair.Dev.Common.Domain.Entities;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Domain.Entities;

[Trait("Category", "Unit")]
public class BaseAuditableEntityTests
{
    private class TestAuditableEntity : BaseAuditableEntity;

    [Fact]
    public void CreatedBy_DefaultValue_ShouldBeEmpty()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.CreatedBy.ShouldBe(string.Empty);
    }

    [Fact]
    public void UpdatedBy_DefaultValue_ShouldBeEmpty()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.UpdatedBy.ShouldBe(string.Empty);
    }

    [Fact]
    public void RecordStatus_DefaultValue_ShouldBeActive()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.RecordStatus.ShouldBe(RecordStatuses.Active);
    }

    [Fact]
    public void UpdatedAt_DefaultValue_ShouldBeNull()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void CreatedAt_DefaultValue_ShouldBeDefault()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.CreatedAt.ShouldBe(default);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var uid = Guid.NewGuid();

        // Act
        var entity = new TestAuditableEntity
        {
            Id = 1,
            UId = uid,
            CreatedAt = now,
            CreatedBy = "creator",
            UpdatedAt = now,
            UpdatedBy = "updater",
            RecordStatus = RecordStatuses.Archived
        };

        // Assert
        entity.Id.ShouldBe(1);
        entity.UId.ShouldBe(uid);
        entity.CreatedAt.ShouldBe(now);
        entity.CreatedBy.ShouldBe("creator");
        entity.UpdatedAt.ShouldBe(now);
        entity.UpdatedBy.ShouldBe("updater");
        entity.RecordStatus.ShouldBe(RecordStatuses.Archived);
    }

    [Fact]
    public void RecordStatus_CanBeSetToDeleted()
    {
        // Act
        var entity = new TestAuditableEntity
        {
            RecordStatus = RecordStatuses.Deleted
        };

        // Assert
        entity.RecordStatus.ShouldBe(RecordStatuses.Deleted);
    }

    [Fact]
    public void InheritsBaseEntity_HasIdAndUId()
    {
        // Act
        var entity = new TestAuditableEntity { Id = 99, UId = Guid.NewGuid() };

        // Assert
        entity.ShouldBeAssignableTo<BaseEntity>();
        entity.Id.ShouldBe(99);
        entity.UId.ShouldNotBe(Guid.Empty);
    }
}
