using JobBoard.Application.Actions.Companies.Get;
using JobBoard.Application.Actions.Companies.Industries;
using JobBoard.Application.Actions.Companies.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Attributes;

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
    [EnableQuery]
    public IActionResult Get()
    {
        var industries = ExecuteODataQueryAsync(new GetIndustriesQuery()).Result;
       
        return Ok(industries);
    }
    
        
  }