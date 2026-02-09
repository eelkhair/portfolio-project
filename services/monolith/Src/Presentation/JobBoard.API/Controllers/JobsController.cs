using JobBoard.Application.Actions.Jobs.Drafts;
using JobBoard.Application.Actions.Jobs.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Controller responsible for handling job-related operations.
/// </summary>
public class JobsController : BaseApiController
{
    /// <summary>
    /// Retrieves a list of job drafts for the specified company.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company for which the drafts should be retrieved.</param>
    /// <returns>An IActionResult containing the list of job drafts or an error response.</returns>
    [HttpGet("{companyId:guid}/list-drafts")]
    public async Task<IActionResult> ListDrafts(Guid companyId) =>
        await ExecuteQueryAsync(new ListDraftsQuery{CompanyId = companyId}, Ok);

    /// <summary>
    /// Updates specific fields of a job draft with new values based on the provided rewrite request.
    /// </summary>
    /// <param name="request">The request containing the field to be updated, its new value, and additional context or style information.</param>
    /// <returns>An IActionResult indicating the success or failure of the operation.</returns>
    [HttpPut("drafts/rewrite")]
    public async Task<IActionResult> RewriteDraftItem(JobRewriteRequest request)
        => await ExecuteCommandAsync(new RewriteDraftItemCommand{JobRewriteRequest = request}, Ok);
}