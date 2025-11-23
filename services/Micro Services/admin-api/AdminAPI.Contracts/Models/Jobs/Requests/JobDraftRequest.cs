namespace AdminAPI.Contracts.Models.Jobs.Requests;

public class JobDraftRequest
{
    public string Title { get; set; } = "";
    public string AboutRole { get; set; } = "";
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
    public string Notes { get; set; } = "";
    public string Location { get; set; } = "";
    public string JobType { get; set; } = "";
    public string SalaryRange { get; set; } = "";
    public JobGenMetadata Metadata { get; set; } = new();
    public string? Id { get; set; } = "";
}