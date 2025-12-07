using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobBoard.API.Infrastructure.OpenApi;

public class TraceIdHeaderOperationFilter : IOperationFilter
{
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