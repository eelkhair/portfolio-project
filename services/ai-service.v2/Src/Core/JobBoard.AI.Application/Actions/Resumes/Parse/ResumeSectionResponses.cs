namespace JobBoard.AI.Application.Actions.Resumes.Parse;

/// <summary>
/// Phase 1 quick parse: contact info, summary, and skills.
/// </summary>
public class ResumeQuickParseResponse
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Linkedin { get; set; } = string.Empty;
    public string Portfolio { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<string> Skills { get; set; } = [];
}

/// <summary>
/// Phase 2: work history section only.
/// </summary>
public class ResumeWorkHistoryParseResponse
{
    public List<ResumeWorkHistoryDto> WorkHistory { get; set; } = [];
}

/// <summary>
/// Phase 2: education section only.
/// </summary>
public class ResumeEducationParseResponse
{
    public List<ResumeEducationDto> Education { get; set; } = [];
}

/// <summary>
/// Phase 2: certifications section only.
/// </summary>
public class ResumeCertificationsParseResponse
{
    public List<ResumeCertificationDto> Certifications { get; set; } = [];
}

/// <summary>
/// Phase 2: projects section only.
/// </summary>
public class ResumeProjectsParseResponse
{
    public List<ResumeProjectDto> Projects { get; set; } = [];
}
