namespace JobBoard.Domain.ValueObjects;

public class WorkHistoryEntry
{
    public string Company { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public bool IsCurrent { get; set; }
}
