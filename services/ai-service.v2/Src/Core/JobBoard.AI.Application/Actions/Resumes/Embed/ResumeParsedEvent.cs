namespace JobBoard.AI.Application.Actions.Resumes.Embed;

public class ResumeParsedEvent
{
    public Guid ResumeUId { get; set; }
    public string UserId { get; set; } = string.Empty;
}
