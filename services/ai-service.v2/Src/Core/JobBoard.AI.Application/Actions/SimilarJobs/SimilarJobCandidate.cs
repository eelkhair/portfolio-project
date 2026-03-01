namespace JobBoard.AI.Application.Actions.SimilarJobs;

public sealed class SimilarJobCandidate
{
    public Guid JobId { get; set; }
    public double Similarity { get; set; }
    public int Rank { get; set; }
}