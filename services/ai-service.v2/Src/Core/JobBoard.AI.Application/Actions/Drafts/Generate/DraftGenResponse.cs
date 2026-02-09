using JobBoard.AI.Application.Actions.Shared;

namespace JobBoard.AI.Application.Actions.Drafts.Generate;

public class DraftGenResponse
{
    public string Title { get; set; } = "";
    public string AboutRole { get; set; } = "";
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
    public string Notes { get; set; } = "";
    public string Location { get; set; } = "";
    public JobMetadata Metadata { get; set; } = new();
    public string DraftId { get; set; } = "";
}