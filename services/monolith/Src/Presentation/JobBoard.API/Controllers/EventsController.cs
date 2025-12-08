using Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using JobBoard.Application.Actions.Companies.Activate;
using JobBoard.Application.Actions.Companies.Models;
using JobBoard.Application.Interfaces.Users;
using JobBoard.infrastructure.Dapr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// The EventsController handles events received via pub/sub messaging mechanisms.
/// It inherits from BaseApiController to leverage shared API functionality.
/// </summary>
public class EventsController(IUserAccessor accessor): BaseApiController
{
    /// <summary>
    /// Handles the "company-created-success" event triggered by a successful company creation operation.
    /// Updates the user context with the UserId from the event.
    /// </summary>
    /// <param name="request">
    /// An object containing the event payload, which includes the UserId, IdempotencyKey,
    /// and company-specific data such as company name, unique ID, and email.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the task to complete, allowing the operation to be cancelled.
    /// </param>
    /// <returns>
    /// An IActionResult indicating the result of the event handling operation, typically an HTTP 200 response.
    /// </returns>
    [HttpPost("company-created-success")]
    [Topic(PubSubNames.RabbitMq, "company.create.success")]
    [Authorize(Policy = "DaprInternal")]
    public async Task<IActionResult> CompanyCreatedSuccess(EventDto<CompanyCreatedModel> request, CancellationToken cancellationToken)
    {
        accessor.UserId = request.UserId;

        await ExecuteCommandAsync(new ActivateCompanyCommand(request.Data), Ok);
        return Ok();
    }
}