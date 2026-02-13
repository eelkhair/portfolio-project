using JobBoard.Monolith.Contracts.Companies;
using JobBoard.Monolith.Contracts.Users;
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
    /// <returns>The configured EDM model for OData endpoints.</returns>
    public static IEdmModel Get()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EnableLowerCamelCase();
        var companySet = builder.EntitySet<CompanyDto>("Companies");
        
        var industrySet = builder.EntitySet<IndustryDto>("Industries");
        var userSet = builder.EntitySet<UserDto>("Users");

        
        companySet.EntityType.HasRequired(c=>c.Industry);
      
       return builder.GetEdmModel();
    }
}