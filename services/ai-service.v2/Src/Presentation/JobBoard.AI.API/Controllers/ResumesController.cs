using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Azure.Storage.Blobs;
using Dapr;
using Dapr.Client;
using JobBoard.AI.Application.Actions.Resumes.Parse;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Resume parsing endpoints
/// </summary>
public class ResumesController : BaseApiController
{
    private const string IdempotencyPrefix = "resume.parse:";
    private const int PendingTtlSeconds = 300;
    private const int CompletedTtlSeconds = 7 * 24 * 3600;
    private const string ResumeContainer = "resumes";

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
        [FromServices] DaprClient daprClient,
        [FromServices] IActivityFactory activityFactory,
        [FromServices] IMonolithApiClient monolithClient,
        [FromServices] BlobServiceClient blobServiceClient,
        [FromServices] ILogger<ResumesController> logger,
        EventDto<ResumeUploadedEvent> request)
    {
        using var activity = activityFactory.StartActivity("parseResumeEvent.IdempotencyCheck", ActivityKind.Internal);
        var stateKey = $"{IdempotencyPrefix}{request.IdempotencyKey}";
        var existing = await daprClient.GetStateAsync<string>(StateStores.Redis, stateKey);

        if (existing is not null)
        {
            activity?.SetTag("idempotency.duplicate", true);
            logger.LogInformation(
                "Skipping resume parse. Idempotency key {IdempotencyKey} already processed",
                request.IdempotencyKey);
            return Accepted();
        }

        activity?.SetTag("idempotency.new", true);
        await daprClient.SaveStateAsync(
            StateStores.Redis, stateKey, "processing",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = PendingTtlSeconds.ToString() });

        var eventData = request.Data;
        userAccessor.UserId = request.UserId;

        try
        {
            logger.LogInformation(
                "Processing resume parse event for {ResumeUId} ({FileName})",
                eventData.ResumeUId, eventData.OriginalFileName);

            // Download resume from blob storage
            var containerClient = blobServiceClient.GetBlobContainerClient(ResumeContainer);
            var blobClient = containerClient.GetBlobClient(eventData.FileName);
            var blobResponse = await blobClient.DownloadStreamingAsync();

            using var ms = new MemoryStream();
            await blobResponse.Value.Content.CopyToAsync(ms);
            var base64Content = Convert.ToBase64String(ms.ToArray());

            // Parse via existing command handler
            var parseRequest = new ResumeParseRequest
            {
                FileName = eventData.OriginalFileName,
                ContentType = eventData.ContentType,
                FileContent = base64Content
            };

            var handler = HttpContext.RequestServices
                .GetRequiredService<IHandler<ParseResumeCommand, ResumeParseResponse>>();

            var parsed = await handler.HandleAsync(new ParseResumeCommand(parseRequest), HttpContext.RequestAborted);

            // Notify monolith of success
            await monolithClient.NotifyResumeParseCompletedAsync(new ResumeParseCompletedRequest
            {
                ResumeUId = eventData.ResumeUId,
                ParsedContent = parsed,
                UserId = request.UserId,
                CurrentPage = eventData.CurrentPage
            }, HttpContext.RequestAborted);

            logger.LogInformation("Resume {ResumeUId} parsed successfully", eventData.ResumeUId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse resume {ResumeUId}", eventData.ResumeUId);

            try
            {
                await monolithClient.NotifyResumeParseFailedAsync(new ResumeParseFailedRequest
                {
                    ResumeUId = eventData.ResumeUId,
                    Reason = ex.Message,
                    UserId = request.UserId,
                    CurrentPage = eventData.CurrentPage
                }, HttpContext.RequestAborted);
            }
            catch (Exception callbackEx)
            {
                logger.LogError(callbackEx,
                    "Failed to notify monolith of parse failure for resume {ResumeUId}",
                    eventData.ResumeUId);
            }

            return Accepted();
        }

        await daprClient.SaveStateAsync(
            StateStores.Redis, stateKey, "done",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = CompletedTtlSeconds.ToString() });

        return Accepted();
    }
}
