using JobBoard.AI.Application.Actions.Shared;
using JobBoard.AI.Domain.Drafts;

namespace JobBoard.AI.Application.Actions.Drafts;

public class DraftResponse
{
    public string Title { get; set; } = "";
    public string AboutRole { get; set; } = "";
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
    public string Notes { get; set; } = "";
    public string Location { get; set; } = "";
    public JobMetadata Metadata { get; set; } = new();
    
    public string JobType { get; set; } = "";
    public string SalaryRange { get; set; } = "";
    public string Id { get; set; } = "";
    
}