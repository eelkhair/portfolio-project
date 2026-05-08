namespace ConnectorAPI.Models.DemoCompany;

/// <summary>
/// Payload forwarded from the monolith DemoCompanyClaimedV1Event to each microservice
/// so they can repoint admin user references and clear their local IsDemo flag.
/// </summary>
public sealed class DemoCompanyClaimedPayload
{
    public Guid NewAdminUserUId { get; set; }
    public string NewAdminEmail { get; set; } = string.Empty;
    public string NewAdminFirstName { get; set; } = string.Empty;
    public string NewAdminLastName { get; set; } = string.Empty;
    public Guid SyntheticAdminUserUId { get; set; }
}
