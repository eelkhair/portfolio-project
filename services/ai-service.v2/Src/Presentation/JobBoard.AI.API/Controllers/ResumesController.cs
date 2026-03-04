using Dapr;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Resumes.DeleteEmbedding;
using JobBoard.AI.Application.Actions.Resumes.Embed;
using JobBoard.AI.Application.Actions.Resumes.MatchingJobs;
using JobBoard.AI.Application.Actions.Resumes.Parse;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Resume parsing and embedding endpoints
/// </summary>
public class ResumesController : BaseApiController
{
    private const string ParseIdempotencyPrefix = "resume.parse:";
    private const string EmbedIdempotencyPrefix = "resume.embed:";
    private const string DeleteEmbeddingIdempotencyPrefix = "resume.delete-embedding:";

    /// <summary>
    /// Parse a resume file and extract structured content (synchronous, Dapr invoke)
    /// </summary>
    [HttpPost("parse")]
    public async Task<IActionResult> Parse([FromBody] ResumeParseRequest request)
        => await ExecuteCommandAsync(new ParseResumeCommand(request), Ok);

    /// <summary>
    /// Handles resume.uploaded events from the monolith outbox via Dapr pub/sub.
    /// Downloads the resume from blob storage, parses it, and calls back the monolith.
    /// </summary>
    [HttpPost("parse-event")]
    [Authorize("DaprInternal")]
    [Topic("rabbitmq.pubsub", "monolith.resume-uploaded.v1")]
    public async Task<IActionResult> ParseResumeEvent(
        [FromServices] IUserAccessor userAccessor,
        EventDto<ResumeUploadedV1Event> request)
    {
        userAccessor.UserId = request.UserId;

        return await ExecuteEventCommandAsync(
            new ProcessResumeUploadedCommand(request),
            request.IdempotencyKey,
            ParseIdempotencyPrefix);
    }

    /// <summary>
    /// Handles resume.parsed events from the monolith outbox via Dapr pub/sub.
    /// Retrieves parsed content, generates embeddings, and stores in Postgres.
    /// </summary>
    [HttpPost("embed-event")]
    [Authorize("DaprInternal")]
    [Topic("rabbitmq.pubsub", "monolith.resume-parsed.v1")]
    public async Task<IActionResult> EmbedResumeEvent(
        [FromServices] IUserAccessor userAccessor,
        EventDto<ResumeParsedV1Event> request)
    {
        userAccessor.UserId = request.UserId;

        return await ExecuteEventCommandAsync(
            new EmbedResumeCommand(request),
            request.IdempotencyKey,
            EmbedIdempotencyPrefix);
    }

    /// <summary>
    /// Handles resume.deleted events from the monolith outbox via Dapr pub/sub.
    /// Removes the corresponding embedding from Postgres.
    /// </summary>
    [HttpPost("delete-embedding-event")]
    [Authorize("DaprInternal")]
    [Topic("rabbitmq.pubsub", "monolith.resume-deleted.v1")]
    public async Task<IActionResult> DeleteResumeEmbeddingEvent(
        [FromServices] IUserAccessor userAccessor,
        EventDto<ResumeDeletedV1Event> request)
    {
        userAccessor.UserId = request.UserId;

        return await ExecuteEventCommandAsync(
            new DeleteResumeEmbeddingCommand(request),
            request.IdempotencyKey,
            DeleteEmbeddingIdempotencyPrefix);
    }

    /// <summary>
    /// Gets a list of matching jobs for a given resume.
    /// </summary>
    /// <param name="resumeId"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    [HttpGet("{resumeId}/matching")]
    [Authorize("DaprInternal")]
    public async Task<IActionResult> GetMatchingJobsForResume([FromRoute] Guid resumeId,
        [FromQuery] int limit = 10)
    => await ExecuteQueryAsync(new ListMatchingJobsQuery(resumeId, limit), Ok);
}
