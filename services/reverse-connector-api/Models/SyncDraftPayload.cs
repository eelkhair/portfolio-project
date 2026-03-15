namespace ReverseConnectorAPI.Models;

public class SyncDraftPayload
{
    public Guid DraftId { get; set; }
    public Guid CompanyId { get; set; }
    public string ContentJson { get; set; } = "{}";
}
