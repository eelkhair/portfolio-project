using Elkhair.Dev.Common.Domain.Entities;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Domain.Entities;

[Trait("Category", "Unit")]
public class BaseEntityTests
{
    private class TestEntity : BaseEntity;

    [Fact]
    public void Id_DefaultValue_ShouldBeZero()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.ShouldBe(0);
    }

    [Fact]
    public void UId_DefaultValue_ShouldBeEmptyGuid()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.UId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Id_CanBeSet()
    {
        // Act
        var entity = new TestEntity { Id = 42 };

        // Assert
        entity.Id.ShouldBe(42);
    }

    [Fact]
    public void UId_CanBeSet()
    {
        // Arrange
        var uid = Guid.NewGuid();

        // Act
        var entity = new TestEntity { UId = uid };

        // Assert
        entity.UId.ShouldBe(uid);
    }
}
