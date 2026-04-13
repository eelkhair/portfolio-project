using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Profiles.Get;

public class GetUserProfileQuery : BaseQuery<UserProfileResponse?>
{
}

public class GetUserProfileQueryHandler(
    IJobBoardQueryDbContext context,
    IActivityFactory activityFactory,
    ILogger<GetUserProfileQueryHandler> logger)
    : BaseQueryHandler(context, logger),
      IHandler<GetUserProfileQuery, UserProfileResponse?>
{
    public async Task<UserProfileResponse?> HandleAsync(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("GetUserProfile", ActivityKind.Internal);

        Logger.LogInformation("Fetching profile for user {UserId}", request.UserId);

        var user = await Context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == request.UserId, cancellationToken);

        if (user is null)
        {
            Logger.LogWarning("User not found for external ID {UserId}", request.UserId);
            activity?.SetTag("profile.user_found", false);
            activity?.SetStatus(ActivityStatusCode.Error, "User not found");
            return null;
        }

        activity?.SetTag("profile.user_uid", user.Id.ToString());
        activity?.SetTag("profile.user_found", true);

        var profile = await Context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.InternalId, cancellationToken);

        var hasProfile = profile is not null;
        activity?.SetTag("profile.exists", hasProfile);

        Logger.LogInformation(
            "Profile lookup complete for user {UserUId}: profile_exists={ProfileExists}",
            user.Id, hasProfile);

        return ProfileMapper.ToResponse(user, profile, hasProfile);
    }
}
