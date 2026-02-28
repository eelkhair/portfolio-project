namespace JobBoard.Monolith.Contracts.Public;

public class PublicCompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? About { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }
    public string IndustryName { get; set; } = string.Empty;
    public int JobCount { get; set; }
}
