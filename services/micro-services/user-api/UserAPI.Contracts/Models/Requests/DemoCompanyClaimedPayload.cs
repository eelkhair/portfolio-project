namespace UserAPI.Contracts.Models.Requests;

/// <summary>
/// Payload sent from connector-api to user-api during the demo claim flow,
/// carrying the real signed-up user's identity so user-api can repoint the admin
/// reference + Keycloak group membership.
/// </summary>
public sealed class DemoCompanyClaimedPayload
{
    public Guid NewAdminUserUId { get; set; }
    public string NewAdminEmail { get; set; } = string.Empty;
    public string NewAdminFirstName { get; set; } = string.Empty;
    public string NewAdminLastName { get; set; } = string.Empty;
    public Guid SyntheticAdminUserUId { get; set; }
}
