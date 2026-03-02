namespace JobBoard.Monolith.Contracts.Public;

public class ResumeResponse
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool HasParsedContent { get; set; }
    public DateTime CreatedAt { get; set; }
}
