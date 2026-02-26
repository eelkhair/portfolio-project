using System.Text.Json;
using JobBoard.Application.Interfaces;
using JobBoard.Domain.Entities.Infrastructure;
using JobBoard.Infrastructure.Outbox;
using JobBoard.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class OutboxPublisherTests
{
    private readonly IOutboxDbContext _outboxDbContext;
    private readonly DbSet<OutboxMessage> _outboxMessages;
    private readonly OutboxPublisher _sut;

    public OutboxPublisherTests()
    {
        _outboxDbContext = Substitute.For<IOutboxDbContext>();
        _outboxMessages = Substitute.For<DbSet<OutboxMessage>>();
        _outboxDbContext.OutboxMessages.Returns(_outboxMessages);

        _sut = new OutboxPublisher(_outboxDbContext);
    }

    [Fact]
    public async Task PublishAsync_ShouldAddMessageToOutboxMessages()
    {
        var integrationEvent = new TestIntegrationEvent("company.created.v1", "user-123");

        await _sut.PublishAsync(integrationEvent, CancellationToken.None);

        await _outboxMessages.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m =>
                m.EventType == "company.created.v1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldSerializePayloadAsJson()
    {
        var integrationEvent = new TestIntegrationEvent("test.event", "user-123");

        await _sut.PublishAsync(integrationEvent, CancellationToken.None);

        await _outboxMessages.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m =>
                m.Payload.Contains("test.event")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldSetEventType()
    {
        var integrationEvent = new TestIntegrationEvent("company.updated.v1", "user-456");

        await _sut.PublishAsync(integrationEvent, CancellationToken.None);

        await _outboxMessages.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m => m.EventType == "company.updated.v1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldSetCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var integrationEvent = new TestIntegrationEvent("test.event", "user-123");

        await _sut.PublishAsync(integrationEvent, CancellationToken.None);
        var after = DateTime.UtcNow;

        await _outboxMessages.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m =>
                m.CreatedAt >= before && m.CreatedAt <= after),
            Arg.Any<CancellationToken>());
    }

    private record TestIntegrationEvent(string EventType, string UserId) : IIntegrationEvent;
}
