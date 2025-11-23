using JobBoard.API.Helpers;
using Swashbuckle.AspNetCore.Filters;

namespace JobBoard.API.SwaggerExamples;

/// <summary>
/// 
/// </summary>
public class ValidationFailureResponseExample : IExamplesProvider<ApiResponse>
{
    /// <summary>
    /// Provides an example of a validation failure response
    /// </summary>
    /// <returns></returns>
    public ApiResponse GetExamples()
    {
        return new ApiResponse
        {
            IsSuccess = false,
            Message = "One or more validation errors occurred.",
            Errors = new Dictionary<string, string[]>
            {
                { "errors", ["Error 1 Details"] }
            }
        };
    }
}

/// <summary>
/// For 401 Unauthorized
/// </summary>
public class UnauthorizedResponseExample : IExamplesProvider<ApiResponse>
{
    /// <summary>
    /// Provides an example of an unauthorized response
    /// </summary>
    /// <returns></returns>
    public ApiResponse GetExamples() => ApiResponse.Fail("User is not authenticated.");
}


/// <summary>
/// Provides an example of a forbidden response
/// </summary>
public class ForbiddenResponseExample : IExamplesProvider<ApiResponse>
{
    /// <summary>
    /// Provides an example of a forbidden response
    /// </summary>
    /// <returns></returns>
    public ApiResponse GetExamples() => ApiResponse.Fail("User does not have permission to perform this action.");
}
