using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr;
using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Jobs;
using JobBoard.AI.Application.Actions.Jobs.Publish;
using JobBoard.AI.Application.Actions.SimilarJobs;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Jobs Controller
/// </summary>
public class JobsController : BaseApiController
{
    private const string IdempotencyPrefix = "job.published:";
    private const int PendingTtlSeconds = 120;
    private const int CompletedTtlSeconds = 7 * 24 * 3600;

    /// <summary>
    /// Publishes a job to the system.
    /// </summary>
    [HttpPost("publish")]
    [Authorize("DaprInternal")]
    [Topic("rabbitmq.pubsub", "job.published.v2")]
    public async Task<IActionResult> PublishJob(
        [FromServices] IUserAccessor userAccessor,
        [FromServices] DaprClient daprClient,
        [FromServices] IActivityFactory activityFactory,
        [FromServices] ILogger<JobsController> logger,
        EventDto<PublishedJobEvent> request)
    {
        using var activity = activityFactory.StartActivity("publishJob.IdempotencyCheck", ActivityKind.Internal);
        var stateKey = $"{IdempotencyPrefix}{request.IdempotencyKey}";
        var existing = await daprClient.GetStateAsync<string>(
            StateStores.Redis, stateKey);

        if (existing is not null)
        {
            activity?.SetTag("idempotency.duplicate", true);
            logger.LogInformation(
                "Skipping job publish. Idempotency key {IdempotencyKey} already processed",
                request.IdempotencyKey);
            return Accepted();
        }

        activity?.SetTag("idempotency.new", true);
        await daprClient.SaveStateAsync(
            StateStores.Redis,
            stateKey,
            "processing",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = PendingTtlSeconds.ToString() });

        try
        {
            userAccessor.UserId = request.UserId;
            await ExecuteCommandAsync(new PublishJobCommand(request), Ok);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process published job {JobUId}",
                request.Data.UId);
            return Accepted();
        }
        activity?.SetTag("idempotency.processed", true);
        await daprClient.SaveStateAsync(
            StateStores.Redis,
            stateKey,
            "done",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = CompletedTtlSeconds.ToString() });

        return Accepted();
    }
    
    [HttpGet("{jobId:guid}/similar")]
    public async Task<IActionResult> GetSimilarJobs(
        Guid jobId,
        [FromQuery] int limit = 5) =>
        await ExecuteQueryAsync(
            new GetSimilarJobsQuery(jobId, limit),
            Ok
        );
}
