namespace JobBoard.Monolith.Contracts.Sync;

public class SyncJobCreateRequest
{
    public Guid JobId { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AboutRole { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? SalaryRange { get; set; }
    public string JobType { get; set; } = string.Empty;
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
    public string UserId { get; set; } = "system";
}
