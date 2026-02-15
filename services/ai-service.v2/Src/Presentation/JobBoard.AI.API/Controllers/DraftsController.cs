using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Actions.Drafts.RewriteItem;
using JobBoard.AI.Application.Actions.Drafts.Save;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Drafts Controller 
/// </summary>
public class DraftsController : BaseApiController
{
    /// <summary>
    /// List job drafts for a company
    /// </summary>
    /// <param name="companyId"></param>
    /// <returns></returns>
    [HttpGet("{companyId:guid}")]
    [StandardApiResponses]
    public async Task<IActionResult> List(Guid companyId)
        => await ExecuteQueryAsync(new ListDraftsQuery(companyId), Ok);
        
    /// <summary>
    /// Generate a job draft using AI
    /// </summary>
    /// <param name="companyId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("{companyId:guid}/generate")]
    [StandardApiResponses]
   
    public async Task<IActionResult> Generate(Guid companyId, GenerateDraftRequest request)
        => await ExecuteCommandAsync(new GenerateDraftCommand(companyId, request), Ok);
    
    /// <summary>
    /// Save a job draft
    /// </summary>
    /// <param name="companyId"></param>
    /// <param name="request"></param>
    [HttpPut("{companyId:guid}/upsert")]
    [StandardApiResponses]
    public async Task<IActionResult> Save(Guid companyId, [FromBody] SaveDraftRequest request)
        => await ExecuteCommandAsync(new SaveDraftCommand(companyId, request), Ok);
    
    
    /// <summary>
    /// Rewrite item
    /// </summary>
    [HttpPut("rewrite/item")]
    [StandardApiResponses]
    public async Task<IActionResult> RewriteItem([FromBody] RewriteItemRequest request)
        => await ExecuteCommandAsync(new RewriteItemCommand(request), Ok);
    
    
}