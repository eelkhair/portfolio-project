namespace JobBoard.Domain.ValueObjects;

public class EducationEntry
{
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}
