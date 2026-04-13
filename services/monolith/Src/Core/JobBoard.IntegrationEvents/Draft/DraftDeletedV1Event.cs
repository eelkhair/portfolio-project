namespace JobBoard.IntegrationEvents.Draft;

public record DraftDeletedV1Event(
    Guid UId,
    Guid CompanyUId
) : IIntegrationEvent
{
    public string EventType => "draft.deleted.v1";
    public string? UserId { get; set; }
}
