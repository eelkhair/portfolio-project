using JobBoard.Domain.Entities.Users;
using JobBoard.Monolith.Contracts.Public;

namespace JobBoard.Application.Actions.Profiles;

internal static class ProfileMapper
{
    public static UserProfileResponse ToResponse(User user, UserProfile profile)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = profile.Phone,
            LinkedIn = profile.LinkedIn,
            Portfolio = profile.Portfolio,
            About = profile.About,
            Skills = profile.Skills?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [],
            PreferredLocation = profile.PreferredLocation,
            PreferredJobType = profile.PreferredJobType,
            WorkHistory = profile.WorkHistory.Select(wh => new WorkHistoryDto
            {
                Company = wh.Company,
                JobTitle = wh.JobTitle,
                StartDate = wh.StartDate,
                EndDate = wh.EndDate,
                Description = wh.Description,
                IsCurrent = wh.IsCurrent
            }).ToList(),
            Education = profile.Education.Select(ed => new EducationDto
            {
                Institution = ed.Institution,
                Degree = ed.Degree,
                FieldOfStudy = ed.FieldOfStudy,
                StartDate = ed.StartDate,
                EndDate = ed.EndDate
            }).ToList(),
            Certifications = profile.Certifications.Select(cert => new CertificationDto
            {
                Name = cert.Name,
                IssuingOrganization = cert.IssuingOrganization,
                IssueDate = cert.IssueDate,
                ExpirationDate = cert.ExpirationDate,
                CredentialId = cert.CredentialId
            }).ToList(),
            Projects = profile.Projects.Select(proj => new ProjectDto
            {
                Name = proj.Name,
                Description = proj.Description,
                Technologies = proj.Technologies,
                Url = proj.Url
            }).ToList()
        };
    }

    public static UserProfileResponse ToResponse(User user, UserProfile? profile, bool profileExists)
    {
        if (!profileExists || profile is null)
        {
            return new UserProfileResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };
        }

        return ToResponse(user, profile);
    }
}
