namespace JobBoard.Monolith.Contracts.Public;

public class SubmitApplicationRequest
{
    public required Guid JobId { get; set; }
    public Guid? ResumeId { get; set; }
    public string? CoverLetter { get; set; }
}
