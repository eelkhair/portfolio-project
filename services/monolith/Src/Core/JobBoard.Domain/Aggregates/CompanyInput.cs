namespace JobBoard.Domain.Aggregates;

public sealed record CompanyInput(
    int InternalId,
    Guid Id,
    string Name,
    string Email,
    string Status,
    int IndustryId,
    string? Description = null,
    string? Website = null,
    string? Logo = null,
    string? Phone = null,
    string? About = null,
    string? EEO = null,
    DateTime? Founded = null,
    string? Size = null,
    string? ExternalId = null,
    DateTime? CreatedAt = null,
    string? CreatedBy = null);
