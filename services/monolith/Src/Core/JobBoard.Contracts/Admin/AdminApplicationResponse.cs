using JobBoard.Monolith.Contracts.Public;

namespace JobBoard.Monolith.Contracts.Admin;

public class AdminApplicationListItem
{
    public Guid Id { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // AI match data (populated when includeMatchScores=true)
    public double? MatchScore { get; set; }
    public string? MatchSummary { get; set; }
    public List<string>? MatchDetails { get; set; }
    public List<string>? MatchGaps { get; set; }
}

public class AdminApplicationDetail : AdminApplicationListItem
{
    public string? CoverLetter { get; set; }
    public Guid? ResumeId { get; set; }
    public PersonalInfoDto? PersonalInfo { get; set; }
    public List<WorkHistoryDto>? WorkHistory { get; set; }
    public List<EducationDto>? Education { get; set; }
    public List<CertificationDto>? Certifications { get; set; }
    public List<string>? Skills { get; set; }
    public List<ProjectDto>? Projects { get; set; }
}

public class UpdateApplicationStatusRequest
{
    public required string Status { get; set; }
}

public class BatchUpdateStatusRequest
{
    public required List<Guid> ApplicationIds { get; set; }
    public required string Status { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
