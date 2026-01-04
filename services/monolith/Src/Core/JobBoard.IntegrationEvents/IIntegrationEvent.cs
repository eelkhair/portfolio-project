

// ReSharper disable UnusedMember.Global
namespace JobBoard.IntegrationEvents;

public interface IIntegrationEvent
{
    string EventType { get; }
    string UserId { get; }
}