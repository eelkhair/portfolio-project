using System.ComponentModel.DataAnnotations;

namespace AdminAPI.Contracts.Models.Companies.Requests;

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

    [Required]
    public string AdminFirstName { get; set; } = string.Empty;

    [Required]
    public string AdminLastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;
    
    public Guid? AdminUserId { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? UserCompanyId { get; set; }
    
    public string? UserId { get; set; }

}