using JobBoard.Application.Actions.Companies.Get;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;

namespace JobBoard.API.Controllers.OData;

/// <summary>
/// The CompaniesController provides OData API endpoints for managing, querying, and interacting
/// with company-related resources. This controller inherits from the BaseODataController
/// to enable common OData functionality.
/// </summary>

public class CompaniesController : BaseODataController
{
    /// <summary>
    /// Retrieves a company by its unique identifier (UId).
    /// </summary>
    /// <returns>A filtered <see cref="SingleResult"/> containing the company matching the specified UId.</returns>
    [HttpGet]
    [EnableQuery]
    [Route("companies")]
    public async Task<IActionResult> Get()
    {
        var companies = await ExecuteODataQueryAsync(new GetCompaniesQuery());
       
        return Ok(companies);
    }

    /// <summary>
    /// Retrieves a company by its unique identifier (id).
    /// </summary>
    /// <param name="id">The unique identifier of the company to retrieve.</param>
    /// <returns>An HTTP response containing the company matching the specified identifier, or a not found result if no matching company is found.</returns>
    [EnableQuery]
    [HttpGet("odata/companies({id})")]
    public async Task<ActionResult> GetById(Guid id)
    {
       var companies = await ExecuteODataQueryAsync(new GetCompaniesQuery());
       var company = companies.FirstOrDefault(c => c.Id == id);
       return Ok(company);
    }

    /// <summary>
    /// Returns all companies with their published job counts.
    /// </summary>
    [HttpGet]
    [EnableQuery]
    [Route("companies/job-summaries")]
    public async Task<IActionResult> GetJobSummaries()
    {
        var summaries = await ExecuteODataQueryAsync(new GetCompanyJobSummariesQuery());
        return Ok(summaries);
    }
  }