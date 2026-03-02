namespace JobBoard.Monolith.Contracts.Public;

public class ResumeParsedContentResponse
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Linkedin { get; set; } = string.Empty;
    public string Portfolio { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
}
