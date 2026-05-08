namespace JobAPI.Contracts.Models.Companies.Requests;

public sealed class DemoCompanyClaimedPayload
{
    public Guid NewAdminUserUId { get; set; }
    public string NewAdminEmail { get; set; } = string.Empty;
    public string NewAdminFirstName { get; set; } = string.Empty;
    public string NewAdminLastName { get; set; } = string.Empty;
    public Guid SyntheticAdminUserUId { get; set; }
}
