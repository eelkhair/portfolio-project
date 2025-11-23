

// ReSharper disable UnusedMember.Global
namespace JobBoard.IntegrationEvents;

public interface IIntegrationEvent
{
    string EventType { get; }
    string TraceId { get; }
}

public static class EventAction
{
    public const string Created = "New";
    public const string Updated = "Update";
    public const string Deleted = "Delete";

    public static string EventType(string action) => action switch
    {
        Created => "Created",
        Updated => "Updated",
        Deleted => "Deleted",
        _ => string.Empty
    };
    // etc.
}