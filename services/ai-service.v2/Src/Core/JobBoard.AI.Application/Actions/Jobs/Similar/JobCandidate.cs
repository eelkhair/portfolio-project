namespace JobBoard.AI.Application.Actions.Jobs.Similar;

public sealed class JobCandidate
{
    public Guid JobId { get; set; }
    public double Similarity { get; set; }
    public int Rank { get; set; }
    public string? MatchSummary { get; set; }
    public List<string>? MatchDetails { get; set; }
    public List<string>? MatchGaps { get; set; }
}