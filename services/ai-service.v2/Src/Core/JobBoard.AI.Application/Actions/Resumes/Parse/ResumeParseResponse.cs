namespace JobBoard.AI.Application.Actions.Resumes.Parse;

public class ResumeParseResponse
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Linkedin { get; set; } = string.Empty;
    public string Portfolio { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
    public List<ResumeWorkHistoryDto> WorkHistory { get; set; } = [];
    public List<ResumeEducationDto> Education { get; set; } = [];
    public List<ResumeCertificationDto> Certifications { get; set; } = [];
    public string? Summary { get; set; }
    public List<ResumeProjectDto> Projects { get; set; } = [];
}

public class ResumeWorkHistoryDto
{
    public string Company { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
    public bool IsCurrent { get; set; }
}

public class ResumeEducationDto
{
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

public class ResumeCertificationDto
{
    public string Name { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public string? IssueDate { get; set; }
    public string? ExpirationDate { get; set; }
    public string? CredentialId { get; set; }
}

public class ResumeProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Technologies { get; set; } = [];
    public string? Url { get; set; }
}
