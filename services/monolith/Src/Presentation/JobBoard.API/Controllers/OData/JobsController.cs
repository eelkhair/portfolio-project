using JobBoard.Application.Actions.Jobs.List;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace JobBoard.API.Controllers.OData;

/// <summary>
/// The JobsController handles OData requests for job-related resources in the system.
/// </summary>
public class JobsController: BaseODataController
{
    /// <summary>
    /// Retrieves a list of jobs for a specific company.
    /// </summary>
    /// <param name="companyId"></param>
    /// <returns></returns>
    [HttpGet]
    [EnableQuery]
    [Route("jobs/{companyId:guid}")]
    public async Task<IActionResult> Get(Guid companyId)
    {
        var jobs = await ExecuteODataQueryAsync(new ListJobsQuery(companyId));
       
        return Ok(jobs);
    }
    
    
}