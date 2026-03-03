namespace JobBoard.Domain.ValueObjects;

public class PersonalInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LinkedIn { get; set; }
    public string? Portfolio { get; set; }
}
