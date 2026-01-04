using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.API.Infrastructure.OpenApi;

/// <summary>
/// Operation filter to add OData query parameters to Swagger documentation.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ODataQueryOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var enableQueryAttribute = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<EnableQueryAttribute>()
            .FirstOrDefault();

        if (enableQueryAttribute == null)
        {
            return;
        }
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$select",
            In = ParameterLocation.Query,
            Description = "Specifies a subset of properties to return (e.g., $select=Name,UId).",
            Schema = new OpenApiSchema { Type = "string" }
        });
        
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$expand",
            In = ParameterLocation.Query,
            Description = "Expands related entities in the response (e.g., $expand=Industries).",
            Schema = new OpenApiSchema { Type = "string" }
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$filter",
            In = ParameterLocation.Query,
            Description = "Filters the results based on a Boolean condition (e.g., $filter=Name eq 'Avengers').",
            Schema = new OpenApiSchema { Type = "string" }
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$orderby",
            In = ParameterLocation.Query,
            Description = "Sorts the results (e.g., $orderby=Name desc).",
            Schema = new OpenApiSchema { Type = "string" }
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$top",
            In = ParameterLocation.Query,
            Description = "Returns only the first n results.",
            Schema = new OpenApiSchema { Type = "integer" }
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$skip",
            In = ParameterLocation.Query,
            Description = "Skips the first n results.",
            Schema = new OpenApiSchema { Type = "integer" }
        });
        
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$count",
            In = ParameterLocation.Query,
            Description = "Includes the total count of items in the results.",
            Schema = new OpenApiSchema { Type = "boolean" }
        });
    }
    
}