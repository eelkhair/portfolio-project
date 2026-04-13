using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities.Users;
using JobBoard.Domain.ValueObjects;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Profiles.Upsert;

public class UpsertUserProfileCommand(UserProfileRequest request) : BaseCommand<UserProfileResponse>
{
    public UserProfileRequest Request { get; set; } = request;
}

public class UpsertUserProfileCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<UpsertUserProfileCommand, UserProfileResponse>
{
    public async Task<UserProfileResponse> HandleAsync(UpsertUserProfileCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("UpsertUserProfile", ActivityKind.Internal);

        var user = await db.Users
            .FirstAsync(u => u.ExternalId == command.UserId, cancellationToken);

        activity?.SetTag("profile.user_uid", user.Id.ToString());
        activity?.SetTag("profile.user_email", user.Email);

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.InternalId, cancellationToken);

        var isCreate = profile is null;
        var operationType = isCreate ? "create" : "update";

        activity?.SetTag("profile.operation", operationType);
        Logger.LogInformation("Upserting profile for user {UserUId} (operation={Operation})", user.Id, operationType);

        var req = command.Request;
        var workHistory = req.WorkHistory?.Select(MapWorkHistory).ToList();
        var education = req.Education?.Select(MapEducation).ToList();
        var certifications = req.Certifications?.Select(MapCertification).ToList();
        var projects = req.Projects?.Select(MapProject).ToList();

        if (profile is not null)
        {
            // Update existing profile via domain methods
            profile.SetPhone(req.Phone);
            profile.SetLinkedIn(req.LinkedIn);
            profile.SetPortfolio(req.Portfolio);
            profile.SetAbout(req.About);
            profile.SetSkills(req.Skills != null ? string.Join(",", req.Skills) : null);
            profile.SetPreferredLocation(req.PreferredLocation);
            profile.SetPreferredJobType(req.PreferredJobType);
            profile.SetWorkHistory(workHistory);
            profile.SetEducation(education);
            profile.SetCertifications(certifications);
            profile.SetProjects(projects);
        }
        else
        {
            // Create new profile via factory method
            var (id, uid) = await Context.GetNextValueFromSequenceAsync(typeof(UserProfile), cancellationToken);

            profile = UserProfile.Create(new UserProfileInput
            {
                UserId = user.InternalId,
                Phone = req.Phone,
                LinkedIn = req.LinkedIn,
                Portfolio = req.Portfolio,
                About = req.About,
                Skills = req.Skills,
                PreferredLocation = req.PreferredLocation,
                PreferredJobType = req.PreferredJobType,
                WorkHistory = workHistory,
                Education = education,
                Certifications = certifications,
                Projects = projects,
                InternalId = id,
                UId = uid,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.UserId
            });

            await db.UserProfiles.AddAsync(profile, cancellationToken);
        }

        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            activity?.SetTag("profile.profile_uid", profile.Id.ToString());
            activity?.SetTag("profile.has_preferred_location", !string.IsNullOrEmpty(profile.PreferredLocation));
            activity?.SetTag("profile.has_preferred_job_type", profile.PreferredJobType.HasValue);

            Logger.LogInformation(
                "Successfully {Operation}d profile {ProfileUId} for user {UserUId}",
                operationType, profile.Id, user.Id);

            return Task.CompletedTask;
        });

        return ProfileMapper.ToResponse(user, profile);
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
