namespace JobBoard.AI.Application.Actions.Resumes.Parse;

public class ResumeParseRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty; // Base64-encoded
}
