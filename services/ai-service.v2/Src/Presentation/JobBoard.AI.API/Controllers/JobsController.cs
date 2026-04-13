using Dapr;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Jobs.Publish;
using JobBoard.AI.Application.Actions.Jobs.Search;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Jobs Controller
/// </summary>
public class JobsController : BaseApiController
{
    private const string IdempotencyPrefix = "job.published:";

    /// <summary>
    /// Publishes a job to the system.
    /// </summary>
    [HttpPost("publish")]
    [Authorize("DaprInternal")]
    [Topic("rabbitmq.pubsub", "job.published.v2")]
    public async Task<IActionResult> PublishJob(
        [FromServices] IUserAccessor userAccessor,
        EventDto<PublishedJobEvent> request)
    {
        userAccessor.UserId = request.UserId;

        return await ExecuteEventCommandAsync(
            new PublishJobCommand(request),
            request.IdempotencyKey,
            IdempotencyPrefix);
    }

    /// <summary>
    /// Get similar jobs to the specified job
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    [HttpGet("{jobId:guid}/similar")]
    [Authorize("DaprInternal")]
    public async Task<IActionResult> GetSimilarJobs(
        Guid jobId,
        [FromQuery] int limit = 5) =>
        await ExecuteQueryAsync(
            new GetSimilarJobsQuery(jobId, limit),
            Ok
        );

    /// <summary>
    /// Searches for jobs based on the specified query and returns a limited number of results.
    /// </summary>
    /// <param name="query">The search query used to find matching jobs.</param>
    /// <param name="location"></param>
    /// <param name="jobType"></param>
    /// <param name="limit">The maximum number of job results to return. Defaults to 30.</param>
    /// <returns>An IActionResult containing the search results, which include a list of job candidates.</returns>
    [HttpGet("search")]
    [Authorize("DaprInternal")]
    public async Task<IActionResult> SearchJobs(
        [FromQuery] string? query,
        [FromQuery] string? location,
        [FromQuery] string? jobType,
        [FromQuery] int limit = 30) =>
        await ExecuteQueryAsync(
            new SearchJobsQuery(query, location, jobType, limit),
            Ok
        );
}
