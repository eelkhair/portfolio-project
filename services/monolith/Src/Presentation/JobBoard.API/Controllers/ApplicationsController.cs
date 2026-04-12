using JobBoard.Application.Actions.Applications.Admin;
using JobBoard.Domain;
using JobBoard.Monolith.Contracts.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Admin endpoints for managing job applications pipeline.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.Dashboard)]
public class ApplicationsController : BaseApiController
{
    /// <summary>
    /// Lists all applications with optional filtering and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] Guid? jobId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return await ExecuteQueryAsync(
            new ListApplicationsQuery(status, jobId, search, page, pageSize), Ok);
    }

    /// <summary>
    /// Gets full details of a specific application.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        return await ExecuteQueryAsync(new GetApplicationDetailQuery(id), Ok);
    }

    /// <summary>
    /// Updates the status of an application (e.g., Submitted → UnderReview → Shortlisted).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateApplicationStatusRequest request)
    {
        return await ExecuteCommandAsync(
            new UpdateApplicationStatusCommand(id, request.Status), Ok);
    }
}
