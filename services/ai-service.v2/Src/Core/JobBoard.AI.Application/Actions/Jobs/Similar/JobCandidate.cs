namespace JobBoard.AI.Application.Actions.Jobs.Similar;

public sealed class JobCandidate
{
    public Guid JobId { get; set; }
    public double Similarity { get; set; }
    public int Rank { get; set; }
}