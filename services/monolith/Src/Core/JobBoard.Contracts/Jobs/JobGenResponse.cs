namespace JobBoard.Monolith.Contracts.Jobs;

public class JobGenResponse
{
    public string Title { get; set; } = "";
    public string AboutRole { get; set; } = "";
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
    public string Notes { get; set; } = "";
    public string Location { get; set; } = "";
    public JobGenMetadata Metadata { get; set; } = new();
    public string DraftId { get; set; } = "";
}