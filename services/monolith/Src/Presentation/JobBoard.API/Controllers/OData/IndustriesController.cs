using JobBoard.Application.Actions.Companies.Industries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace JobBoard.API.Controllers.OData;

/// <summary>
/// Provides OData endpoints to manage and retrieve industry data.
/// </summary>
public class IndustriesController : BaseODataController
{
    /// <summary>
    /// Handles HTTP GET requests to retrieve a collection of industries.
    /// </summary>
    /// <returns>An IActionResult containing the retrieved collection of industries in response to an OData query.</returns>
    [HttpGet]
    [EnableQuery]
    [Route("industries")]
    public async Task<IActionResult> Get()
    {
        var industries = await ExecuteODataQueryAsync(new GetIndustriesQuery());
       
        return Ok(industries);
    }
    
        
  }