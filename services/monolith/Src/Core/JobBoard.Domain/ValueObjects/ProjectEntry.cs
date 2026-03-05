namespace JobBoard.Domain.ValueObjects;

public class ProjectEntry
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Technologies { get; set; } = [];
    public string? Url { get; set; }
}
