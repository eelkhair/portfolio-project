using System.Net;
using JobBoard.API.Helpers;
using JobBoard.Application.Actions.Companies.Models;
using Swashbuckle.AspNetCore.Filters;

namespace JobBoard.API.SwaggerExamples;

/// <summary>
/// Provides an example response for creating a company used in Swagger documentation.
/// Implements <see cref="IExamplesProvider{T}"/> to supply example data of type <see cref="ApiResponse{CompanyDto}"/>.
/// </summary>
public class CreateCompanyExample : IExamplesProvider<ApiResponse<CompanyDto>>
{
    /// <summary>
    /// Provides example data for the creation of a company, used for API documentation and testing purposes.
    /// </summary>
    /// <returns>An instance of <see cref="ApiResponse{T}"/> containing pre-defined example data for a company.</returns>
    public ApiResponse<CompanyDto> GetExamples()
    {
        var response = ApiResponse.Success(
            new CompanyDto
            {
                Id = new Guid("019ad2af-3a93-7c0d-bfaf-aa44f50bfa2e"),
                Name = "TechNova Solutions",
                Email = "info@technova.com",
                Website = "https://technova.com",
                Status = "Provisioning"
            }
        );
        response.StatusCode = HttpStatusCode.Created;
        
        return response;
    }
}