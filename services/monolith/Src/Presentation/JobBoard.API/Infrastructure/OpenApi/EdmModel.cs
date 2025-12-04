using JobBoard.Application.Actions.Companies.Models;
using JobBoard.Domain.Entities;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace JobBoard.API.Infrastructure.OpenApi;

/// <summary>
/// Builds the EDM model for OData endpoints.
/// </summary>
public static class EdmModel
{
    /// <summary>
    /// Gets the EDM model.
    /// </summary>
    /// <returns></returns>
    public static IEdmModel Get()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EnableLowerCamelCase();
        builder.EntitySet<CompanyDto>("Companies");
        builder.EntitySet<IndustryDto>("Industries");
        return builder.GetEdmModel();
    }
}