using Dapr.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace JobBoard.API.Controllers;

/// <summary>
/// Manages operations related to the outbox, enabling functionality for processing
/// and handling events.
/// </summary>
[Route("")]
public class OutboxController : BaseApiController
{
    /// <summary>
    /// Process OutboxMessages
    /// </summary>
    /// <returns></returns>
    [HttpPost("process-outbox-messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = "DaprInternal")]
    public async Task<ActionResult> ProcessMessages([FromServices] DaprClient daprClient)
    {
        await daprClient.PublishEventAsync("pubsub.kafka", "outbox-events", new { Name="Test" });
        return Ok();
    }
}