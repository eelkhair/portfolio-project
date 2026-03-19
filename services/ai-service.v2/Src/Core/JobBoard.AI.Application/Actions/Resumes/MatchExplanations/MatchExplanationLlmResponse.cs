namespace JobBoard.AI.Application.Actions.Resumes.MatchExplanations;

public class MatchExplanationLlmResponse
{
    public List<JobExplanationItem> Explanations { get; set; } = [];
}

public class JobExplanationItem
{
    public Guid JobId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Details { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
}
