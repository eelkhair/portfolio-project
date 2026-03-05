namespace JobBoard.Monolith.Contracts.Public;

public class ResumeEmbeddedModel
{
    public Guid ResumeUId { get; set; }
    public string UserId { get; set; } = string.Empty;
}
