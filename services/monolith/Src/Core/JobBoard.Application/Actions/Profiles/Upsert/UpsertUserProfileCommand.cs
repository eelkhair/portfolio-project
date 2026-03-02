using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities.Users;
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

        if (profile is not null)
        {
            // Update existing profile via domain methods
            profile.SetPhone(req.Phone);
            profile.SetLinkedIn(req.LinkedIn);
            profile.SetPortfolio(req.Portfolio);
            profile.SetExperience(req.Experience);
            profile.SetSkills(req.Skills != null ? string.Join(",", req.Skills) : null);
            profile.SetPreferredLocation(req.PreferredLocation);
            profile.SetPreferredJobType(req.PreferredJobType);
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
                Experience = req.Experience,
                Skills = req.Skills,
                PreferredLocation = req.PreferredLocation,
                PreferredJobType = req.PreferredJobType,
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

        return new UserProfileResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = profile.Phone,
            LinkedIn = profile.LinkedIn,
            Portfolio = profile.Portfolio,
            Experience = profile.Experience,
            Skills = profile.Skills?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [],
            PreferredLocation = profile.PreferredLocation,
            PreferredJobType = profile.PreferredJobType
        };
    }
}
