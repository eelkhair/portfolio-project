namespace JobBoard.Monolith.Contracts.Public;

public class ApplicationResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public PersonalInfoDto? PersonalInfo { get; set; }
    public List<WorkHistoryDto>? WorkHistory { get; set; }
    public List<EducationDto>? Education { get; set; }
    public List<CertificationDto>? Certifications { get; set; }
    public List<string>? Skills { get; set; }
    public List<ProjectDto>? Projects { get; set; }
}
