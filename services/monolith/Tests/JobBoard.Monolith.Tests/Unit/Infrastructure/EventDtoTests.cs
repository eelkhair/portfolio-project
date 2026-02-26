using JobBoard.infrastructure.Dapr;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class EventDtoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var data = new { Id = 1, Name = "Test" };

        var dto = new EventDto<object>("user-1", "idempotency-key-1", data);

        dto.UserId.ShouldBe("user-1");
        dto.IdempotencyKey.ShouldBe("idempotency-key-1");
        dto.Data.ShouldBe(data);
    }

    [Fact]
    public void Constructor_ShouldSetCreatedToUtcNow()
    {
        var before = DateTime.UtcNow;

        var dto = new EventDto<string>("user-1", "key-1", "payload");

        var after = DateTime.UtcNow;
        dto.Created.ShouldBeInRange(before, after);
    }

    [Fact]
    public void Properties_ShouldBeMutable()
    {
        var dto = new EventDto<string>("orig-user", "orig-key", "orig-data");

        dto.UserId = "new-user";
        dto.IdempotencyKey = "new-key";
        dto.Data = "new-data";
        dto.Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        dto.UserId.ShouldBe("new-user");
        dto.IdempotencyKey.ShouldBe("new-key");
        dto.Data.ShouldBe("new-data");
        dto.Created.ShouldBe(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Constructor_WithGenericType_ShouldWorkWithDifferentTypes()
    {
        var intDto = new EventDto<int>("user", "key", 42);
        var listDto = new EventDto<List<string>>("user", "key", ["a", "b"]);

        intDto.Data.ShouldBe(42);
        listDto.Data.Count.ShouldBe(2);
    }
}
