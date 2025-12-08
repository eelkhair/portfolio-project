namespace ConnectorAPI.Models;

public class CompanyCreatedPayload
{
    public Guid CompanyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string? CompanyWebsite { get; init; }
    public Guid IndustryUId { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminFirstName { get; init; } = string.Empty;
    public string AdminLastName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public Guid UserCompanyId { get; set; }
    public string? UserId { get; set; }
}