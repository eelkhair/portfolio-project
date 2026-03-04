using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.List;

public class GetUserResumesQuery : BaseQuery<List<ResumeResponse>>
{
}

public class GetUserResumesQueryHandler(
    IJobBoardQueryDbContext context,
    IActivityFactory activityFactory,
    ILogger<GetUserResumesQueryHandler> logger)
    : BaseQueryHandler(context, logger),
      IHandler<GetUserResumesQuery, List<ResumeResponse>>
{
    public async Task<List<ResumeResponse>> HandleAsync(GetUserResumesQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("GetUserResumes", ActivityKind.Internal);

        Logger.LogInformation("Fetching resumes for user {UserId}", request.UserId);

        var user = await Context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == request.UserId, cancellationToken);

        if (user is null)
        {
            Logger.LogWarning("User not found for external ID {UserId}", request.UserId);
            activity?.SetTag("resume.user_found", false);
            activity?.SetStatus(ActivityStatusCode.Error, "User not found");
            return [];
        }

        activity?.SetTag("resume.user_uid", user.Id.ToString());
        activity?.SetTag("resume.user_found", true);

        var resumes = await Context.Resumes
            .Where(r => r.UserId == user.InternalId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ResumeResponse
            {
                Id = r.Id,
                OriginalFileName = r.OriginalFileName,
                ContentType = r.ContentType,
                FileSize = r.FileSize,
                HasParsedContent = r.ParsedContent != null,
                ParseStatus = r.ParseStatus.ToString(),
                ParseRetryCount = r.ParseRetryCount,
                IsDefault = r.IsDefault,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        activity?.SetTag("resume.count", resumes.Count);

        Logger.LogInformation("Found {ResumeCount} resumes for user {UserUId}", resumes.Count, user.Id);

        return resumes;
    }
}
