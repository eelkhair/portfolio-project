using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.Job;
using JobBoard.Monolith.Contracts.Jobs;

namespace JobBoard.Domain.Entities;

public class Job : BaseAuditableEntity
{
    protected Job()
    {
        Title = string.Empty;
        Location = string.Empty;
        AboutRole = string.Empty;
    }
    
    private Job(string title, string location, string aboutRole)
    {
        Title = title;
        Location = location;
        AboutRole = aboutRole;
    }

    public string Title { get; private set; }
    public string Location { get; private set; }
    public JobType JobType { get; private set; }
    public string AboutRole { get; private set; }
    public string? SalaryRange { get; private set; }

    public int CompanyId { get; private set; }
    internal void SetCompany(int companyId) => CompanyId = companyId;

    public Company Company { get; private set; } = null!;

    private readonly List<Responsibility> _responsibilities = [];
    public IReadOnlyCollection<Responsibility> Responsibilities => _responsibilities.AsReadOnly();

    private readonly List<Qualification> _qualifications = [];
    public IReadOnlyCollection<Qualification> Qualifications => _qualifications.AsReadOnly();
    
    public void SetTitle(string title) =>
        Title = JobTitle.Create(title).Ensure<JobTitle, string>("Job.InvalidTitle")!;

    public void SetLocation(string location) =>
        Location = JobLocation.Create(location).Ensure<JobLocation, string>("Job.InvalidLocation")!;

    public void SetAboutRole(string aboutRole) =>
        AboutRole = JobAboutRole.Create(aboutRole).Ensure<JobAboutRole, string>("Job.InvalidAboutRole")!;

    public void SetSalaryRange(string? salaryRange) =>
        SalaryRange = JobSalaryRange.Create(salaryRange).Ensure<JobSalaryRange, string?>("Job.InvalidSalaryRange")!;

    public void SetJobType(JobType jobType) => JobType = jobType;
    
    public void AddResponsibility(Responsibility responsibility)
    {
        if (responsibility is null)
        {
            throw new DomainException(
                "Job.NullResponsibility",
                [ new Error("Job.NullResponsibility", "Responsibility cannot be null.") ]);
        }
        responsibility.SetJob(InternalId);
        _responsibilities.Add(responsibility);
    }

    public void AddQualification(Qualification qualification)
    {
        if (qualification is null)
        {
            throw new DomainException(
                "Job.NullQualification",
                [ new Error("Job.NullQualification", "Qualification cannot be null.") ]);
        }

        qualification.SetJob(InternalId);
        _qualifications.Add(qualification);
    }

    public void RemoveResponsibility(Responsibility responsibility)
    {
        if (responsibility is null) return;

        _responsibilities.Remove(responsibility);
        responsibility.SetJob(0);
    }

    public void RemoveQualification(Qualification qualification)
    {
        if (qualification is null) return;

        _qualifications.Remove(qualification);
        qualification.SetJob(0);
    }
    
    public static Job Create(JobInput jobInput)
    {
        var job = ValidateAndCreate(jobInput);

        job.SetCompany(jobInput.CompanyId);
        EntityFactory.ApplyAudit(job, jobInput.CreatedAt, jobInput.CreatedBy);

        if (jobInput.Responsibilities is not null)
        {
            foreach (var responsibility in jobInput.Responsibilities.Select(r => Responsibility.Create(
                         r,
                         jobInput.CreatedAt,
                         jobInput.CreatedBy
                     )))
            {
                job.AddResponsibility(responsibility);
            }
        }

        if (jobInput.Qualifications is not null)
        {
            foreach (var qualification in jobInput.Qualifications.Select(q => Qualification.Create(
                         q,
                         jobInput.CreatedAt,
                         jobInput.CreatedBy
                     )))
            {
                job.AddQualification(qualification);
            }
        }
 
        job.InternalId = jobInput.InternalId;
        job.Id = jobInput.UId;
        
        return job;
    }

    private static Job ValidateAndCreate(JobInput jobInput)
    {
        var errors = new List<Error>();

        var title = JobTitle.Create(jobInput.Title).Collect<JobTitle, string>(errors)!;
        var location = JobLocation.Create(jobInput.Location).Collect<JobLocation, string>(errors)!;
        var aboutRole = JobAboutRole.Create(jobInput.AboutRole).Collect<JobAboutRole, string>(errors)!;
        var salaryRange = JobSalaryRange.Create(jobInput.SalaryRange).Collect<JobSalaryRange, string?>(errors)!;

        DomainGuard.AgainstInvalidId(jobInput.CompanyId, "Job.InvalidCompanyId", errors);

        if (!Enum.IsDefined(jobInput.JobType))
        {
            errors.Add(new Error("Job.InvalidJobType", "Invalid job type."));
        }

        if (errors.Count > 0)
            throw new DomainException("Job.InvalidEntity", errors);

        return new Job(title, location, aboutRole)
        {
            JobType = jobInput.JobType,
            SalaryRange = salaryRange
        };
    }
}
