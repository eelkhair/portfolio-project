using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Admin;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Applications.Admin;

public class GetApplicationDetailQuery(Guid applicationId) : BaseQuery<AdminApplicationDetail>
{
    public Guid ApplicationId { get; } = applicationId;
}

public class GetApplicationDetailQueryHandler(
    IJobBoardQueryDbContext context,
    ILogger<GetApplicationDetailQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetApplicationDetailQuery, AdminApplicationDetail>
{
    public async Task<AdminApplicationDetail> HandleAsync(
        GetApplicationDetailQuery query, CancellationToken cancellationToken)
    {
        var app = await Context.JobApplications
            .Include(a => a.Job).ThenInclude(j => j.Company)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == query.ApplicationId, cancellationToken)
            ?? throw new NotFoundException($"Application {query.ApplicationId} not found");

        return new AdminApplicationDetail
        {
            Id = app.Id,
            ApplicantName = $"{app.User.FirstName} {app.User.LastName}".Trim(),
            ApplicantEmail = app.User.Email,
            JobId = app.Job.Id,
            JobTitle = app.Job.Title,
            CompanyName = app.Job.Company.Name,
            Status = app.Status.ToString(),
            CreatedAt = app.CreatedAt,
            UpdatedAt = app.UpdatedAt,
            CoverLetter = app.CoverLetter,
            ResumeId = app.Resume?.Id,
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = app.PersonalInfo.FirstName,
                LastName = app.PersonalInfo.LastName,
                Email = app.PersonalInfo.Email,
                Phone = app.PersonalInfo.Phone,
                LinkedIn = app.PersonalInfo.LinkedIn,
                Portfolio = app.PersonalInfo.Portfolio
            },
            WorkHistory = app.WorkHistory.Select(w => new WorkHistoryDto
            {
                Company = w.Company,
                JobTitle = w.JobTitle,
                StartDate = w.StartDate,
                EndDate = w.EndDate,
                Description = w.Description,
                IsCurrent = w.IsCurrent
            }).ToList(),
            Education = app.Education.Select(e => new EducationDto
            {
                Institution = e.Institution,
                Degree = e.Degree,
                FieldOfStudy = e.FieldOfStudy,
                StartDate = e.StartDate,
                EndDate = e.EndDate
            }).ToList(),
            Certifications = app.Certifications.Select(c => new CertificationDto
            {
                Name = c.Name,
                IssuingOrganization = c.IssuingOrganization,
                IssueDate = c.IssueDate,
                ExpirationDate = c.ExpirationDate,
                CredentialId = c.CredentialId
            }).ToList(),
            Skills = app.Skills,
            Projects = app.Projects.Select(p => new ProjectDto
            {
                Name = p.Name,
                Description = p.Description,
                Technologies = p.Technologies,
                Url = p.Url
            }).ToList()
        };
    }
}
