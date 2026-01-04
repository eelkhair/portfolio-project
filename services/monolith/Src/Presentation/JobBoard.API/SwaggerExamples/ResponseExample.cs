using JobBoard.API.Helpers;
using Swashbuckle.AspNetCore.Filters;

namespace JobBoard.API.SwaggerExamples;

/// <summary>
/// Provides an example of a validation failure response
/// </summary>
public class ValidationFailureResponseExample : IExamplesProvider<ApiResponse<object>>
{
    /// <summary>
    /// Provides an example response for a given scenario.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="ApiResponse{T}"/> containing an example response.
    /// </returns>
    public ApiResponse<object> GetExamples()
    {
        return ApiResponse.Fail<object>(
            message: "Validation failed.",
            status: System.Net.HttpStatusCode.BadRequest,
            errors: new Dictionary<string, string[]>
            {
                { "FieldName", new[] { "Error 1 details" } }
            }
        );
    }
}


/// <summary>
/// For 401 Unauthorized
/// </summary>
public class UnauthorizedResponseExample : IExamplesProvider<ApiResponse<object>>
{
    /// <summary>
    /// Provides an example response for a given scenario.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="ApiResponse{T}"/> containing an example response.
    /// </returns>
    public ApiResponse<object> GetExamples()
    {
        return ApiResponse.Fail<object>(
            message: "User is not authenticated.",
            status: System.Net.HttpStatusCode.Unauthorized
        );
    }
}


/// <summary>
/// Provides an example of a forbidden response
/// </summary>

public class ForbiddenResponseExample : IExamplesProvider<ApiResponse<object>>
{
    /// <summary>
    /// Provides an example response for a given scenario.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="ApiResponse{T}"/> containing an example response.
    /// </returns>
    public ApiResponse<object> GetExamples()
    {
        return ApiResponse.Fail<object>(
            message: "User does not have permission to perform this action.",
            status: System.Net.HttpStatusCode.Forbidden
        );
    }
}
