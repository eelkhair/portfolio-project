using JobBoard.Domain.Entities.Infrastructure;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class OutboxEntityTests
{
    [Fact]
    public void OutboxMessage_ShouldInitializeRequiredProperties()
    {
        var msg = new OutboxMessage
        {
            EventType = "CompanyCreated",
            Payload = """{"id": 1}"""
        };

        msg.EventType.ShouldBe("CompanyCreated");
        msg.Payload.ShouldBe("""{"id": 1}""");
    }

    [Fact]
    public void OutboxMessage_ShouldHaveDefaultValues()
    {
        var msg = new OutboxMessage
        {
            EventType = "Test",
            Payload = "{}"
        };

        msg.ProcessedAt.ShouldBeNull();
        msg.RetryCount.ShouldBe(0);
        msg.LastError.ShouldBeNull();
        msg.TraceParent.ShouldBeNull();
    }

    [Fact]
    public void OutboxMessage_MutableProperties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var msg = new OutboxMessage
        {
            EventType = "Test",
            Payload = "{}"
        };

        msg.ProcessedAt = now;
        msg.RetryCount = 3;
        msg.LastError = "Connection timeout";

        msg.ProcessedAt.ShouldBe(now);
        msg.RetryCount.ShouldBe(3);
        msg.LastError.ShouldBe("Connection timeout");
    }

    [Fact]
    public void OutboxDeadLetter_ShouldInitializeRequiredProperties()
    {
        var dl = new OutboxDeadLetter
        {
            OutboxMessageId = 42,
            EventType = "CompanyCreated",
            Payload = """{"id": 42}"""
        };

        dl.OutboxMessageId.ShouldBe(42);
        dl.EventType.ShouldBe("CompanyCreated");
        dl.Payload.ShouldBe("""{"id": 42}""");
    }

    [Fact]
    public void OutboxDeadLetter_ShouldHaveDefaultValues()
    {
        var dl = new OutboxDeadLetter
        {
            EventType = "Test",
            Payload = "{}"
        };

        dl.LastError.ShouldBeNull();
        dl.TraceParent.ShouldBeNull();
        dl.IsEmailSent.ShouldBeFalse();
    }

    [Fact]
    public void OutboxDeadLetter_IsEmailSent_ShouldBeMutable()
    {
        var dl = new OutboxDeadLetter
        {
            EventType = "Test",
            Payload = "{}"
        };

        dl.IsEmailSent = true;

        dl.IsEmailSent.ShouldBeTrue();
    }

    [Fact]
    public void OutboxArchivedMessage_ShouldInitializeAllProperties()
    {
        var now = DateTime.UtcNow;
        var archived = new OutboxArchivedMessage
        {
            OutboxMessageId = 100,
            EventType = "JobPublished",
            Payload = """{"jobId": 5}""",
            ProcessedAt = now,
            RetryCount = 1,
            LastError = null,
            TraceParent = "00-abc123-def456-01"
        };

        archived.OutboxMessageId.ShouldBe(100);
        archived.EventType.ShouldBe("JobPublished");
        archived.Payload.ShouldBe("""{"jobId": 5}""");
        archived.ProcessedAt.ShouldBe(now);
        archived.RetryCount.ShouldBe(1);
        archived.LastError.ShouldBeNull();
        archived.TraceParent.ShouldBe("00-abc123-def456-01");
    }

    [Fact]
    public void OutboxArchivedMessage_DefaultValues_ShouldBeCorrect()
    {
        var archived = new OutboxArchivedMessage
        {
            EventType = "Test",
            Payload = "{}"
        };

        archived.OutboxMessageId.ShouldBe(0);
        archived.ProcessedAt.ShouldBeNull();
        archived.RetryCount.ShouldBe(0);
        archived.LastError.ShouldBeNull();
        archived.TraceParent.ShouldBeNull();
    }
}
