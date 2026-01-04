using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobBoard.API.Infrastructure.OpenApi;

/// <summary>
/// An operation filter that adds a "x-trace-id" header to the response
/// definitions of all API endpoints. This header is used to include a
/// Trace ID for distributed tracing purposes.
/// </summary>
/// <remarks>
/// The "x-trace-id" header is added to every response definition for
/// all documented operations in the API. This header indicates a string
/// value representing a unique identifier for tracing requests through
/// distributed systems.
/// </remarks>
/// <remarks>
/// This filter is typically applied during Swagger/Swashbuckle configuration
/// and ensures that the generated OpenAPI documentation includes the
/// "x-trace-id" header in its response descriptions.
/// </remarks>
/// <example>
/// When applied, the OpenAPI specification for an endpoint will include
/// the "x-trace-id" header under the response section. This adds clarity
/// to API consumers about the availability of distributed tracing headers.
/// </example>
public class TraceIdHeaderOperationFilter : IOperationFilter
{
    /// <summary>
    /// Adds the "x-trace-id" header to the response definitions of an API operation
    /// for distributed tracing purposes.
    /// </summary>
    /// <param name="operation">
    /// Represents the OpenAPI operation being modified. This includes the list
    /// of responses, parameters, and other details of the API endpoint.
    /// </param>
    /// <param name="context">
    /// Provides contextual information about the current API operation,
    /// including details such as the API description and method information.
    /// </param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.TryAdd("200", new OpenApiResponse { Description = "Success" });

        foreach (var response in operation.Responses)
        {
            response.Value.Headers.Add("x-trace-id", new OpenApiHeader
            {
                Description = "Trace ID for distributed tracing",
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}