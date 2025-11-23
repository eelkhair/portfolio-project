namespace UserAPI.Contracts.Models.Responses;

public class UserResponse
{
    public Guid UId { get; set; }
    public string? Email { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}