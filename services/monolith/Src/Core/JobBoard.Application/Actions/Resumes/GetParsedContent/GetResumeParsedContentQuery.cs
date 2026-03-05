using System.Diagnostics;
using System.Text.Json;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.GetParsedContent;

public class GetResumeParsedContentQuery(Guid resumeId) : BaseQuery<ResumeParsedContentResponse?>
{
    public Guid ResumeId { get; set; } = resumeId;
}

public class GetResumeParsedContentQueryHandler(
    IJobBoardQueryDbContext context,
    IActivityFactory activityFactory,
    ILogger<GetResumeParsedContentQueryHandler> logger)
    : BaseQueryHandler(context, logger),
      IHandler<GetResumeParsedContentQuery, ResumeParsedContentResponse?>
{
    public async Task<ResumeParsedContentResponse?> HandleAsync(GetResumeParsedContentQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("GetResumeParsedContent", ActivityKind.Internal);
        activity?.SetTag("resume.resume_id", request.ResumeId.ToString());

        Logger.LogInformation("Fetching parsed content for resume {ResumeId} for user {UserId}",
            request.ResumeId, request.UserId);

        var user = await Context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == request.UserId, cancellationToken);

        if (user is null)
        {
            Logger.LogWarning("User not found for external ID {UserId}", request.UserId);
            activity?.SetStatus(ActivityStatusCode.Error, "User not found");
            throw new NotFoundException("User", request.UserId!);
        }

        activity?.SetTag("resume.user_uid", user.Id.ToString());

        var resume = await Context.Resumes
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId && r.UserId == user.InternalId,
                cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeId} not found for user {UserUId}",
                request.ResumeId, user.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Resume not found");
            throw new NotFoundException($"Resume {request.ResumeId} not found.");
        }

        if (string.IsNullOrEmpty(resume.ParsedContent))
        {
            activity?.SetTag("resume.has_parsed_content", false);
            Logger.LogInformation("Resume {ResumeId} has no parsed content", request.ResumeId);
            return null;
        }

        activity?.SetTag("resume.has_parsed_content", true);

        var parsed = JsonSerializer.Deserialize<ResumeParsedContentResponse>(resume.ParsedContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Logger.LogInformation("Successfully retrieved parsed content for resume {ResumeId}", request.ResumeId);

        return parsed;
    }
}
