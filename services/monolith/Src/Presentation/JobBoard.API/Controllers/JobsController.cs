using JobBoard.Application.Actions.Jobs.Drafts;
using JobBoard.Application.Actions.Jobs.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Controller responsible for handling job-related operations.
/// </summary>
public class JobsController : BaseApiController
{
    [HttpGet("{companyId:guid}/list-drafts")]
    public async Task<IActionResult> ListDrafts(Guid companyId) =>
        await ExecuteQueryAsync(new ListDraftsQuery{CompanyId = companyId}, Ok);
}