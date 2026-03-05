using System.Text.Json;

namespace JobBoard.Monolith.Contracts.Public;

public class ResumeSectionParsedModel
{
    public Guid ResumeUId { get; set; }
    public string Section { get; set; } = string.Empty;
    public JsonElement SectionContent { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}
