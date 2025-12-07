namespace ConnectorAPI.Models;

public class CompanyCreatedPayload
{
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string? Website { get; init; }
    public Guid IndustryId { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminFirstName { get; init; } = string.Empty;
    public string AdminLastName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
}