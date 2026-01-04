using JobBoard.Application.Actions.Companies.Get;
using JobBoard.Application.Actions.Companies.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Attributes;

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
    [EnableQuery]
    public IActionResult Get()
    {
        var companies = ExecuteODataQueryAsync(new GetCompaniesQuery()).Result;
       
        return Ok(companies);
    }

    /// <summary>
    /// Retrieves a company by its unique identifier (id).
    /// </summary>
    /// <param name="id">The unique identifier of the company to retrieve.</param>
    /// <returns>An HTTP response containing the company matching the specified identifier, or a not found result if no matching company is found.</returns>
    [EnableQuery]
    [HttpGet("odata/companies({id})")]
    public ActionResult GetById(Guid id)
    {
       var companies = ExecuteODataQueryAsync(new GetCompaniesQuery()).Result;
       var company = companies.FirstOrDefault(c => c.Id == id);
       return Ok(company);
    }
        
  }