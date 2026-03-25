using Elkhair.Dev.Common.Dapr;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Dapr;

[Trait("Category", "Unit")]
public class EventDtoTests
{
    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var userId = "user-123";
        var idempotencyKey = "key-456";
        var data = new { Name = "Test" };

        // Act
        var dto = new EventDto<object>(userId, idempotencyKey, data);

        // Assert
        dto.UserId.ShouldBe(userId);
        dto.IdempotencyKey.ShouldBe(idempotencyKey);
        dto.Data.ShouldBe(data);
        dto.Created.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Constructor_WithStringData_SetsCorrectType()
    {
        // Arrange
        var data = "string-data";

        // Act
        var dto = new EventDto<string>("user", "key", data);

        // Assert
        dto.Data.ShouldBe("string-data");
    }

    [Fact]
    public void Constructor_WithListData_SetsCorrectType()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };

        // Act
        var dto = new EventDto<List<int>>("user", "key", data);

        // Assert
        dto.Data.Count.ShouldBe(3);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        // Arrange
        var dto = new EventDto<string>("user1", "key1", "data1");

        // Act
        dto.UserId = "user2";
        dto.IdempotencyKey = "key2";
        dto.Data = "data2";
        var newDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        dto.Created = newDate;

        // Assert
        dto.UserId.ShouldBe("user2");
        dto.IdempotencyKey.ShouldBe("key2");
        dto.Data.ShouldBe("data2");
        dto.Created.ShouldBe(newDate);
    }

    [Fact]
    public void Created_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var dto = new EventDto<string>("user", "key", "data");

        // Assert
        var after = DateTime.UtcNow;
        dto.Created.ShouldBeGreaterThanOrEqualTo(before);
        dto.Created.ShouldBeLessThanOrEqualTo(after.AddMilliseconds(100));
    }

    [Fact]
    public void Constructor_WithNullData_SetsNullData()
    {
        // Act
        var dto = new EventDto<string?>("user", "key", null);

        // Assert
        dto.Data.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithEmptyStrings_SetsEmptyValues()
    {
        // Act
        var dto = new EventDto<string>(string.Empty, string.Empty, string.Empty);

        // Assert
        dto.UserId.ShouldBe(string.Empty);
        dto.IdempotencyKey.ShouldBe(string.Empty);
        dto.Data.ShouldBe(string.Empty);
    }
}
