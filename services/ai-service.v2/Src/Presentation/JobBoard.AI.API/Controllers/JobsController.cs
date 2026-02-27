using Dapr;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Jobs Controller
/// </summary>
public class JobsController : BaseApiController
{
    /// <summary>
    /// Publishes a job to the system.
    /// </summary>
    /// <param name="request">The even
    /// t data transfer object containing details about the job to be published, including metadata such as user ID and idempotency key, as well as job-specific details.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IActionResult indicating the result of the publish operation.</returns>
    [HttpPost("publish")]
    [Authorize("DaprInternal")]
    [Topic("rabbitmq.pubsub", "job.published")]
    public async Task<IActionResult> PublishJob(EventDto<PublishedJobEvent> request)
        => await ExecuteCommandAsync(new PublishJobCommand(request), Ok);
}