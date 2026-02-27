using JobBoard.Application.Actions.Drafts.Generate;
using JobBoard.Application.Actions.Drafts.List;
using JobBoard.Application.Actions.Drafts.Rewrite;
using JobBoard.Application.Actions.Drafts.Save;
using JobBoard.Application.Actions.Jobs.Create;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Controller responsible for handling job-related operations.
/// </summary>
public class JobsController : BaseApiController
{
    /// <summary>
    /// Create a new job listing
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateJobRequest request) =>
     await ExecuteCommandAsync(new CreateJobCommand(request), Ok);
    
    /// <summary>
    /// Generate a job draft via AI
    /// </summary>
    [HttpPost("{companyId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid companyId, DraftGenRequest request)
        => await ExecuteCommandAsync(new GenerateDraftCommand
        {
            CompanyId = companyId,
            Request = request
        }, Ok);

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
    /// <param name="companyId"></param>
    /// <param name="request">The request containing the field to be updated, its new value, and additional context or style information.</param>
    /// <returns>An IActionResult indicating the success or failure of the operation.</returns>
    [HttpPut("{companyId:guid}/save-draft")]
    public async Task<IActionResult> SaveDraft(Guid companyId, DraftResponse request)
        => await ExecuteCommandAsync(new SaveDraftCommand
        {
            CompanyId = companyId,
            Draft = request
        }, Ok);

    /// <summary>
    /// Rewrites a job draft with new content based on the provided rewrite request.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("drafts/rewrite")]
    public async Task<IActionResult> RewriteDraftItem(DraftItemRewriteRequest request)
        => await ExecuteCommandAsync(new RewriteDraftItemCommand{DraftItemRewriteRequest = request}, Ok);

}