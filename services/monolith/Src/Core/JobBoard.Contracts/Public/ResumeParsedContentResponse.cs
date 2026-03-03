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
}
