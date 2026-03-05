namespace JobBoard.Domain.ValueObjects;

public class CertificationEntry
{
    public string Name { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public string? IssueDate { get; set; }
    public string? ExpirationDate { get; set; }
    public string? CredentialId { get; set; }
}
