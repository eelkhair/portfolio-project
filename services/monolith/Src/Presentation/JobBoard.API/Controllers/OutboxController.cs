using Dapr.Client;
using JobBoard.Application.Actions.Outbox;
using JobBoard.Application.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace JobBoard.API.Controllers;

/// <summary>
/// Manages operations related to the outbox, enabling functionality for processing
/// and handling events.
/// </summary>
[Route("")]
public class OutboxController(IUserAccessor accessor) : BaseApiController
{
    /// <summary>
    /// Process OutboxMessages
    /// </summary>
    /// <returns></returns>
    [HttpPost("process-outbox-messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = "DaprInternal")]
    public async Task<ActionResult> ProcessMessages()
    {
        accessor.UserId = "OutboxProcessor";
        accessor.FirstName = "OutboxProcessor";
        accessor.LastName = "OutboxProcessor";
        accessor.Email = "OutboxProcessor@eelkhair.net";
        accessor.Roles = new List<string>{"OutboxProcessor"};
        await ExecuteCommandAsync(new ProcessOutboxMessageCommand(), Ok);
        return Ok();
    }
}