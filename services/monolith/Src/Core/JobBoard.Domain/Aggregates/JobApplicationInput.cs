using JobBoard.Domain.ValueObjects;

namespace JobBoard.Domain.Aggregates;

public class JobApplicationInput
{
    public int JobId { get; set; }
    public int UserId { get; set; }
    public int? ResumeId { get; set; }
    public string? CoverLetter { get; set; }

    public PersonalInfo? PersonalInfo { get; set; }
    public List<WorkHistoryEntry>? WorkHistory { get; set; }
    public List<EducationEntry>? Education { get; set; }
    public List<CertificationEntry>? Certifications { get; set; }
    public List<string>? Skills { get; set; }

    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int InternalId { get; set; }
    public Guid UId { get; set; }
}
