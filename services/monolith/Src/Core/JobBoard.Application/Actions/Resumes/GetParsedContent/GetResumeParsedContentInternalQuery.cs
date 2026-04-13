using System.Diagnostics;
using System.Text.Json;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.GetParsedContent;

/// <summary>
/// Internal query for service-to-service retrieval of parsed resume content.
/// Unlike <see cref="GetResumeParsedContentQuery"/>, this does not enforce user ownership.
/// </summary>
public class GetResumeParsedContentInternalQuery(Guid resumeId) : BaseQuery<ResumeParsedContentResponse?>, IAnonymousRequest
{
    public Guid ResumeId { get; set; } = resumeId;
}

public class GetResumeParsedContentInternalQueryHandler(
    IJobBoardQueryDbContext context,
    IActivityFactory activityFactory,
    ILogger<GetResumeParsedContentInternalQueryHandler> logger)
    : BaseQueryHandler(context, logger),
      IHandler<GetResumeParsedContentInternalQuery, ResumeParsedContentResponse?>
{
    public async Task<ResumeParsedContentResponse?> HandleAsync(GetResumeParsedContentInternalQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("GetResumeParsedContentInternal", ActivityKind.Internal);
        activity?.SetTag("resume.resume_id", request.ResumeId.ToString());

        Logger.LogInformation("Internal: Fetching parsed content for resume {ResumeId}", request.ResumeId);

        var resume = await Context.Resumes
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeId} not found", request.ResumeId);
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
