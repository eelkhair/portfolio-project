using System.ComponentModel.DataAnnotations;

namespace CompanyAPI.Contracts.Models.Companies.Requests;
public class CreateCompanyRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CompanyEmail { get; set; } = string.Empty;

    [Url]
    public string? CompanyWebsite { get; set; }

    [Required]
    public Guid IndustryUId { get; set; }
    public Guid? CompanyId { get; init; }
    
    public string? UserId { get; set; }
    
}
    
