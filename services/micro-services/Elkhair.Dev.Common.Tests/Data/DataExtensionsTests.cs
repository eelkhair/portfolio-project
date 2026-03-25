using Elkhair.Dev.Common.Data;
using Elkhair.Dev.Common.Domain.Constants;
using Elkhair.Dev.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Data;

[Trait("Category", "Unit")]
public class DataExtensionsTests
{
    private class TestEntity : BaseEntity;

    private class TestAuditableEntity : BaseAuditableEntity;

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<TestAuditableEntity> TestAuditableEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(builder =>
            {
                builder.ConfigureBaseEntity();
            });

            modelBuilder.Entity<TestAuditableEntity>(builder =>
            {
                builder.ConfigureBaseAuditableEntity();
            });
        }
    }

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=FakeDbForModelBuilding;Trusted_Connection=True;")
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public void ConfigureBaseEntity_SetsUIdDefaultValueSql()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestEntity));

        // Assert
        entityType.ShouldNotBeNull();
        var uidProperty = entityType.FindProperty(nameof(BaseEntity.UId));
        uidProperty.ShouldNotBeNull();
        uidProperty.GetDefaultValueSql().ShouldBe("newsequentialid()");
    }

    [Fact]
    public void ConfigureBaseEntity_SetsTemporal()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestEntity));

        // Assert
        entityType.ShouldNotBeNull();
        entityType.IsTemporal().ShouldBeTrue();
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_SetsCreatedAtColumnType()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        entityType.ShouldNotBeNull();
        var prop = entityType.FindProperty(nameof(BaseAuditableEntity.CreatedAt));
        prop.ShouldNotBeNull();
        prop.GetColumnType().ShouldBe("datetime2");
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_SetsUpdatedAtColumnType()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        var prop = entityType!.FindProperty(nameof(BaseAuditableEntity.UpdatedAt));
        prop.ShouldNotBeNull();
        prop.GetColumnType().ShouldBe("datetime2");
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_SetsCreatedByMaxLength()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        var prop = entityType!.FindProperty(nameof(BaseAuditableEntity.CreatedBy));
        prop.ShouldNotBeNull();
        prop.GetMaxLength().ShouldBe(50);
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_SetsUpdatedByMaxLength()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        var prop = entityType!.FindProperty(nameof(BaseAuditableEntity.UpdatedBy));
        prop.ShouldNotBeNull();
        prop.GetMaxLength().ShouldBe(50);
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_SetsRecordStatusMaxLength()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        var prop = entityType!.FindProperty(nameof(BaseAuditableEntity.RecordStatus));
        prop.ShouldNotBeNull();
        prop.GetMaxLength().ShouldBe(25);
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_SetsRecordStatusDefaultValue()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        var prop = entityType!.FindProperty(nameof(BaseAuditableEntity.RecordStatus));
        prop.ShouldNotBeNull();
        prop.GetDefaultValue().ShouldBe(RecordStatuses.Active);
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_HasQueryFilter()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        entityType.ShouldNotBeNull();
        entityType.GetQueryFilter().ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_InheritsTemporalFromBaseEntity()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        entityType.ShouldNotBeNull();
        entityType.IsTemporal().ShouldBeTrue();
    }

    [Fact]
    public void ConfigureBaseAuditableEntity_InheritsUIdDefaultValueSql()
    {
        // Arrange & Act
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));

        // Assert
        var uidProperty = entityType!.FindProperty(nameof(BaseEntity.UId));
        uidProperty.ShouldNotBeNull();
        uidProperty.GetDefaultValueSql().ShouldBe("newsequentialid()");
    }
}
