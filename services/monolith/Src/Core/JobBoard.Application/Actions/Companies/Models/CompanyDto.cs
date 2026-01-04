using JobBoard.Application.Actions.Base;

namespace JobBoard.Application.Actions.Companies.Models;

public class CompanyDto : BaseDto
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
    
    public IndustryDto? Industry { get; set; }
    public Guid IndustryId { get; set; }
}