namespace JobBoard.Monolith.Contracts.Sync;

public class SyncDraftRequest
{
    public Guid DraftId { get; set; }
    public Guid CompanyId { get; set; }
    public string ContentJson { get; set; } = "{}";
    public string UserId { get; set; } = "system";
}
