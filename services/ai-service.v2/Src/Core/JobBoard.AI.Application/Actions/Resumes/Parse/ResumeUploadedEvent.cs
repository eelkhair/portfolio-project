namespace JobBoard.AI.Application.Actions.Resumes.Parse;

public class ResumeUploadedEvent
{
    public Guid ResumeUId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}
