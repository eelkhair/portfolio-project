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
       var companySet = builder.EntitySet<CompanyDto>("Companies");
       var industrySet = builder.EntitySet<IndustryDto>("Industries");
       
       companySet.EntityType.HasKey(c => c.UId);
       industrySet.EntityType.HasKey(c => c.UId);
        return builder.GetEdmModel();
    }
}