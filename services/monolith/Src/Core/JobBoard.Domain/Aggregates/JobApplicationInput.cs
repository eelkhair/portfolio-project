namespace JobBoard.Domain.Aggregates;

public class JobApplicationInput
{
    public int JobId { get; set; }
    public int UserId { get; set; }
    public int? ResumeId { get; set; }
    public string? CoverLetter { get; set; }

    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int InternalId { get; set; }
    public Guid UId { get; set; }
}
