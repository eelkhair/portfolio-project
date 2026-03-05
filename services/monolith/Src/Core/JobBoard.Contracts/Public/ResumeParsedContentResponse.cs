namespace JobBoard.Monolith.Contracts.Public;

public class ResumeParsedContentResponse
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Linkedin { get; set; } = string.Empty;
    public string Portfolio { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
    public List<WorkHistoryDto> WorkHistory { get; set; } = [];
    public List<EducationDto> Education { get; set; } = [];
    public List<CertificationDto> Certifications { get; set; } = [];
    public string? Summary { get; set; }
    public List<ProjectDto> Projects { get; set; } = [];
}

public class ProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Technologies { get; set; } = [];
    public string? Url { get; set; }
}
