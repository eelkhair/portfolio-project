namespace CompanyAPI.Contracts.Models.Industries.Responses;

public class IndustryResponse
{
    public Guid UId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; } 
}