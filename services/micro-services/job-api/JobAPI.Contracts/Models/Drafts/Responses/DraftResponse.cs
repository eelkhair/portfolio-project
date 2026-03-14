namespace JobAPI.Contracts.Models.Drafts.Responses;

public class DraftResponse
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string AboutRole { get; set; } = "";
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
    public string Notes { get; set; } = "";
    public string Location { get; set; } = "";
    public string JobType { get; set; } = "";
    public string SalaryRange { get; set; } = "";
}
