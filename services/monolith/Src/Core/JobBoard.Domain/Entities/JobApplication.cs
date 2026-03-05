using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities.Users;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects;

namespace JobBoard.Domain.Entities;

public class JobApplication : BaseAuditableEntity
{
    protected JobApplication()
    {
    }

    public int JobId { get; private set; }
    public Job Job { get; private set; } = null!;

    public int UserId { get; private set; }
    public User User { get; private set; } = null!;

    public int? ResumeId { get; private set; }
    public Resume? Resume { get; private set; }

    public string? CoverLetter { get; private set; }
    public ApplicationStatus Status { get; private set; }

    public PersonalInfo PersonalInfo { get; private set; } = new();
    public List<WorkHistoryEntry> WorkHistory { get; private set; } = [];
    public List<EducationEntry> Education { get; private set; } = [];
    public List<CertificationEntry> Certifications { get; private set; } = [];
    public List<string> Skills { get; private set; } = [];
    public List<ProjectEntry> Projects { get; private set; } = [];

    internal void SetJob(int jobId) => JobId = jobId;
    internal void SetUser(int userId) => UserId = userId;
    public void SetStatus(ApplicationStatus status) => Status = status;

    public static JobApplication Create(JobApplicationInput input)
    {
        var errors = new List<Error>();

        DomainGuard.AgainstInvalidId(input.JobId, "Application.InvalidJobId", errors);
        DomainGuard.AgainstInvalidId(input.UserId, "Application.InvalidUserId", errors);

        if (errors.Count > 0)
            throw new DomainException("Application.InvalidEntity", errors);

        var application = new JobApplication
        {
            CoverLetter = input.CoverLetter?.Trim(),
            Status = ApplicationStatus.Submitted,
            ResumeId = input.ResumeId,
            PersonalInfo = input.PersonalInfo ?? new PersonalInfo(),
            WorkHistory = input.WorkHistory ?? [],
            Education = input.Education ?? [],
            Certifications = input.Certifications ?? [],
            Skills = input.Skills ?? [],
            Projects = input.Projects ?? []
        };

        application.SetJob(input.JobId);
        application.SetUser(input.UserId);
        application.InternalId = input.InternalId;
        application.Id = input.UId;

        EntityFactory.ApplyAudit(application, input.CreatedAt, input.CreatedBy);

        return application;
    }
}

public enum ApplicationStatus
{
    Submitted,
    UnderReview,
    Shortlisted,
    Rejected,
    Accepted
}
