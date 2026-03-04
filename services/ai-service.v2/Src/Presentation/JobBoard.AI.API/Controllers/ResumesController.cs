using Dapr;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Resumes.Embed;
using JobBoard.AI.Application.Actions.Resumes.Parse;
using JobBoard.AI.Application.Interfaces.Configurations;
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
        EventDto<ResumeUploadedEvent> request)
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
        EventDto<ResumeParsedEvent> request)
    {
        userAccessor.UserId = request.UserId;

        return await ExecuteEventCommandAsync(
            new EmbedResumeCommand(request),
            request.IdempotencyKey,
            EmbedIdempotencyPrefix);
    }
}
