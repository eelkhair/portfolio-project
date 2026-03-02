namespace JobBoard.Domain.Aggregates;

public class ResumeInput
{
    public int UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public string? ParsedContent { get; set; }

    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int InternalId { get; set; }
    public Guid UId { get; set; }
}
