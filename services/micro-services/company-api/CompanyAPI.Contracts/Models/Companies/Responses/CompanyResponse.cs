namespace CompanyAPI.Contracts.Models.Companies.Responses;

public class CompanyResponse : BaseDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? About { get; set; }
    public string? EEO { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid IndustryUId { get; set; }
}