using ConnectorAPI.Models;
using Shouldly;

namespace connector_api.tests.Unit.Models;

[Trait("Category", "Unit")]
public class EventDtoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var userId = "user-123";
        var idempotencyKey = Guid.NewGuid().ToString();
        var data = new { Name = "Test" };

        var dto = new EventDto<object>(userId, idempotencyKey, data);

        dto.UserId.ShouldBe(userId);
        dto.IdempotencyKey.ShouldBe(idempotencyKey);
        dto.Data.ShouldBe(data);
    }

    [Fact]
    public void Constructor_ShouldSetCreatedToUtcNow()
    {
        var before = DateTime.UtcNow;

        var dto = new EventDto<string>("user", "key", "data");

        var after = DateTime.UtcNow;
        dto.Created.ShouldBeGreaterThanOrEqualTo(before);
        dto.Created.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Properties_ShouldBeMutable()
    {
        var dto = new EventDto<string>("user", "key", "original");

        dto.UserId = "new-user";
        dto.Data = "updated";
        dto.IdempotencyKey = "new-key";
        dto.Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        dto.UserId.ShouldBe("new-user");
        dto.Data.ShouldBe("updated");
        dto.IdempotencyKey.ShouldBe("new-key");
        dto.Created.ShouldBe(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Constructor_ShouldWorkWithDifferentGenericTypes()
    {
        var stringDto = new EventDto<string>("u", "k", "hello");
        var intDto = new EventDto<int>("u", "k", 42);
        var complexDto = new EventDto<List<string>>("u", "k", ["a", "b"]);

        stringDto.Data.ShouldBe("hello");
        intDto.Data.ShouldBe(42);
        complexDto.Data.Count.ShouldBe(2);
    }
}
