namespace JobBoard.Application.Actions.Jobs.MatchingJobs;

public class MatchingJobResponse
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AboutRole { get; set; }
    public string? SalaryRange { get; set; }
    public double Similarity { get; set; }
    public string? MatchSummary { get; set; }
    public List<string>? MatchDetails { get; set; }
    public List<string>? MatchGaps { get; set; }
}
