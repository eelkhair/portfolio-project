// ReSharper disable NotAccessedPositionalProperty.Global

using System.Diagnostics;

namespace JobBoard.IntegrationEvents;

public record ConsensusAlleleEvent(
    long Id,
    Guid UId,
    string Action,
    string Name,
    string Sequence,
    DateTime CreatedAt,
    DateTime? UpdatedAt) : IIntegrationEvent
{
    public string EventType { get; } = $"ConsensusAllele{EventAction.EventType(Action)}Event";
    public string TraceId { get; } = Activity.Current?.TraceId.ToString() ?? string.Empty;
}