// StandardResponsesOperationFilter.cs

using System.Text.Json;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobBoard.API.Infrastructure.OpenApi;

/// <summary>
/// Operation filter to add standard API responses based on the presence of the StandardApiResponsesAttribute.
/// </summary>
/// <param name="serviceProvider"></param>
public class StandardResponsesOperationFilter(IServiceProvider serviceProvider) : IOperationFilter
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// Applies the operation filter to add standard responses.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo
                .GetCustomAttributes(typeof(StandardApiResponsesAttribute), false)
                .FirstOrDefault() is not StandardApiResponsesAttribute standardResponsesAttribute)
        {
            return;
        }

        if (standardResponsesAttribute.Include400BadRequest)
        {
            AddResponseWithExample(context, serviceProvider, operation, "400", "Bad Request", typeof(ValidationFailureResponseExample));
        }
        

        var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length != 0;
        if (hasAllowAnonymous) return;
        AddResponseWithExample(context, serviceProvider, operation, "401", "Unauthorized", typeof(UnauthorizedResponseExample));
        AddResponseWithExample(context, serviceProvider, operation, "403", "Forbidden", typeof(ForbiddenResponseExample));
    }

    private void AddResponseWithExample(OperationFilterContext context, IServiceProvider provider, OpenApiOperation operation, string statusCode, string description, Type? exampleProviderType)
    {
        var response = new OpenApiResponse { Description = description };

        if (exampleProviderType != null)
        {
            dynamic? exampleProvider = provider.GetService(exampleProviderType);
            if (exampleProvider != null)
            {
                object example = exampleProvider.GetExamples();
                var jsonString = JsonSerializer.Serialize(example, example.GetType(), _jsonSerializerOptions);
                var openApiExample = new OpenApiString(jsonString);

                response.Content.Add("application/json", new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(example.GetType(), context.SchemaRepository),
                    Example = openApiExample
                });
            }
        }
        
        operation.Responses.TryAdd(statusCode, response);
    }
}