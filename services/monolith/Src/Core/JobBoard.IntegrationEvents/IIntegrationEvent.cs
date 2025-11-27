

// ReSharper disable UnusedMember.Global
namespace JobBoard.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    string EventType { get; }
    string Action { get; }
    string TraceId { get; }
    DateTime OccurredAt { get; }
}