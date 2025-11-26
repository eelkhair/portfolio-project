namespace JobBoard.Domain.Aggregates;

public sealed record CompanyInput( string Name,
    string Email,
    string Status,
    string? Description,
    string? Website,
    string? Logo,
    string? Phone,
    string? About, 
    string? EEO,
    DateTime? Founded,
    string? Size,
    long IndustryId);