namespace JobAPI.Contracts.Models.Companies.Responses;

public class CompanyResponse
{
    public Guid UId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? About { get; set; }
    public string? EEO { get; set; }  
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; } 
}