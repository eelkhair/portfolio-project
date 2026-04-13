using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.ValueObjects;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Applications.Submit;

public class SubmitApplicationCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<SubmitApplicationCommand, ApplicationResponse>
{
    public async Task<ApplicationResponse> HandleAsync(SubmitApplicationCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("SubmitApplication", ActivityKind.Internal);

        var req = command.Request;

        var user = await db.Users
            .FirstAsync(u => u.ExternalId == command.UserId, cancellationToken);

        var job = await db.Jobs
            .Include(j => j.Company)
            .FirstAsync(j => j.Id == req.JobId, cancellationToken);

        activity?.SetTag("application.user_uid", user.Id.ToString());
        activity?.SetTag("application.job_uid", job.Id.ToString());
        activity?.SetTag("application.job_title", job.Title);

        Logger.LogInformation("Submitting application for job {JobUId} by user {UserUId}", job.Id, user.Id);

        int? resumeInternalId = null;
        if (req.ResumeId.HasValue)
        {
            var resume = await db.Resumes
                .FirstOrDefaultAsync(r => r.Id == req.ResumeId.Value && r.UserId == user.InternalId, cancellationToken);
            resumeInternalId = resume?.InternalId;
        }

        var workHistory = req.WorkHistory?.Select(MapWorkHistory).ToList();
        var education = req.Education?.Select(MapEducation).ToList();
        var certifications = req.Certifications?.Select(MapCertification).ToList();
        var projects = req.Projects?.Select(MapProject).ToList();

        var (id, uid) = await Context.GetNextValueFromSequenceAsync(typeof(JobApplication), cancellationToken);

        var application = JobApplication.Create(new JobApplicationInput
        {
            JobId = job.InternalId,
            UserId = user.InternalId,
            ResumeId = resumeInternalId,
            CoverLetter = req.CoverLetter,
            PersonalInfo = req.PersonalInfo != null ? new PersonalInfo
            {
                FirstName = req.PersonalInfo.FirstName,
                LastName = req.PersonalInfo.LastName,
                Email = req.PersonalInfo.Email,
                Phone = req.PersonalInfo.Phone,
                LinkedIn = req.PersonalInfo.LinkedIn,
                Portfolio = req.PersonalInfo.Portfolio
            } : null,
            WorkHistory = workHistory,
            Education = education,
            Certifications = certifications,
            Skills = req.Skills,
            Projects = projects,
            InternalId = id,
            UId = uid,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = command.UserId
        });

        await db.JobApplications.AddAsync(application, cancellationToken);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            activity?.SetTag("application.application_uid", application.Id.ToString());
            Logger.LogInformation(
                "Successfully submitted application {ApplicationUId} for job {JobUId} by user {UserUId}",
                application.Id, job.Id, user.Id);
            return Task.CompletedTask;
        });

        return new ApplicationResponse
        {
            Id = application.Id,
            JobId = job.Id,
            JobTitle = job.Title,
            CompanyName = job.Company.Name,
            Status = application.Status.ToString(),
            CreatedAt = application.CreatedAt,
            PersonalInfo = req.PersonalInfo,
            WorkHistory = req.WorkHistory,
            Education = req.Education,
            Certifications = req.Certifications,
            Skills = req.Skills,
            Projects = req.Projects
        };
    }

    private static WorkHistoryEntry MapWorkHistory(WorkHistoryDto dto) => new()
    {
        Company = dto.Company,
        JobTitle = dto.JobTitle,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        Description = dto.Description,
        IsCurrent = dto.IsCurrent
    };

    private static EducationEntry MapEducation(EducationDto dto) => new()
    {
        Institution = dto.Institution,
        Degree = dto.Degree,
        FieldOfStudy = dto.FieldOfStudy,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
    };

    private static CertificationEntry MapCertification(CertificationDto dto) => new()
    {
        Name = dto.Name,
        IssuingOrganization = dto.IssuingOrganization,
        IssueDate = dto.IssueDate,
        ExpirationDate = dto.ExpirationDate,
        CredentialId = dto.CredentialId
    };

    private static ProjectEntry MapProject(ProjectDto dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description,
        Technologies = dto.Technologies ?? [],
        Url = dto.Url
    };
}
