using System.Diagnostics;

namespace JobBoard.IntegrationEvents.Company;

public record CompanyCreatedV1Event(
    Guid CompanyUId,
    string Name,
    string Email,
    string Status,
    string? Website,
    Guid IndustryUId,
    AdminUserInfo Admin
) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public string EventType => "company.created.v1";
    public string Action => "created";
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string TraceId => Activity.Current?.TraceId.ToString() ?? string.Empty;
}

public record AdminUserInfo(
    Guid UserUId,
    string FirstName,
    string LastName,
    string Email
);