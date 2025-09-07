namespace CompanyAPI.Contracts.Models.Companies.Requests;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? About { get; set; }
    public string? EEO { get; set; }  
    
}