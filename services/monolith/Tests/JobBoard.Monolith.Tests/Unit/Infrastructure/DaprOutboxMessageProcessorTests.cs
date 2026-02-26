using System.Text.Json;
using Dapr.Client;
using JobBoard.Domain.Entities.Infrastructure;
using JobBoard.infrastructure.Dapr;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class DaprOutboxMessageProcessorTests
{
    private readonly DaprClient _daprClient;
    private readonly DaprOutboxMessageProcessor _sut;

    public DaprOutboxMessageProcessorTests()
    {
        _daprClient = Substitute.For<DaprClient>();
        _sut = new DaprOutboxMessageProcessor(_daprClient);
    }

    [Fact]
    public async Task ProcessAsync_ShouldPublishToCorrectTopic()
    {
        var message = CreateMessage("company.created.v1", """{"id": 1}""");

        await _sut.ProcessAsync(message, CancellationToken.None);

        await _daprClient.Received(1).PublishEventAsync(
            "rabbitmq.pubsub",
            "outbox-events",
            Arg.Any<EventDto<object>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ShouldDeserializePayloadIntoEventDto()
    {
        var payload = """{"name": "TestCorp"}""";
        var message = CreateMessage("company.created.v1", payload);

        await _sut.ProcessAsync(message, CancellationToken.None);

        await _daprClient.Received(1).PublishEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<EventDto<object>>(e => e.Data != null),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ShouldSetUserIdFromCreatedBy()
    {
        var message = CreateMessage("test.event", """{}""", "user-42");

        await _sut.ProcessAsync(message, CancellationToken.None);

        await _daprClient.Received(1).PublishEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<EventDto<object>>(e => e.UserId == "user-42"),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ShouldSetIdempotencyKey()
    {
        var message = CreateMessage("test.event", """{}""");

        await _sut.ProcessAsync(message, CancellationToken.None);

        await _daprClient.Received(1).PublishEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<EventDto<object>>(e => !string.IsNullOrEmpty(e.IdempotencyKey)),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    private static OutboxMessage CreateMessage(string eventType, string payload, string createdBy = "test-user")
        => new()
        {
            EventType = eventType,
            Payload = payload,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
}
