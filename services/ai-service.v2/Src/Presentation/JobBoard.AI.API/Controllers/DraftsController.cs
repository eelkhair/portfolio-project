using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Drafts.Save;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Drafts Controller 
/// </summary>
public class DraftsController : BaseApiController
{
    /// <summary>
    /// Generate a job draft using AI
    /// </summary>
    /// <param name="companyId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("{companyId:guid}/generate")]
    [AllowAnonymous]
    [StandardApiResponses]
   
    public async Task<IActionResult> Generate(Guid companyId, DraftGenRequest request)
        => await ExecuteCommandAsync(new DraftGenCommand(companyId, request), Ok);
    
    /// <summary>
    /// Save a job draft
    /// </summary>
    /// <param name="companyId"></param>
    /// <param name="request"></param>
    [HttpPut("{companyId:guid}/upsert")]
    [AllowAnonymous]
    [StandardApiResponses]
    public async Task<IActionResult> Save(Guid companyId, [FromBody] SaveDraftRequest request)
        => await ExecuteCommandAsync(new SaveDraftCommand(companyId, request), Ok);
    
}