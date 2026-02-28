using System.ComponentModel.DataAnnotations;

namespace CompanyAPI.Contracts.Models.Companies.Requests;

public class UpdateCompanyRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CompanyEmail { get; set; } = string.Empty;

    [Url]
    public string? CompanyWebsite { get; set; }

    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? About { get; set; }
    public string? EEO { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }

    [Required]
    public Guid IndustryUId { get; set; }
}
