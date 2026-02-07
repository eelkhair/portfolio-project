using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobBoard.AI.API.Infrastructure.OpenApi;

/// <summary>
/// An operation filter that adds an "x-trace-id" header to the response
/// definitions of all API endpoints. This header is used to expose a
/// distributed trace identifier to API consumers.
/// </summary>
/// <remarks>
/// The "x-trace-id" header is included in every documented response,
/// allowing clients to correlate requests with server-side logs
/// and distributed tracing systems (e.g., OpenTelemetry).
/// </remarks>
public sealed class TraceIdHeaderOperationFilter : IOperationFilter
{
    /// <summary>
    /// Adds the "x-trace-id" header to all responses for the given API operation.
    /// </summary>
    /// <param name="operation">The OpenAPI operation being modified.</param>
    /// <param name="context">Contextual metadata about the API operation.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Ensure at least one response exists so headers can be attached
        operation.Responses?.TryAdd("200", new OpenApiResponse
        {
            Description = "Success"
        });

        if (operation.Responses is null) return;

        foreach (var response in operation.Responses.Values)
        {
            response.Headers?.TryAdd("x-trace-id", new OpenApiHeader
            {
                Description = "Distributed trace identifier for correlating logs and traces",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                }
            });
        }
    }
}
