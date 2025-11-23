// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace JobBoard.API.Helpers;

/// <summary>
/// Standard API response structure
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates if the API call was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    /// <summary>
    /// Message providing additional information about the API response
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Validation errors, if any
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Creates a successful ApiResponse
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static ApiResponse Success(string? message = null)
    {
        return new ApiResponse { IsSuccess = true, Message = message };
    }

    /// <summary>
    /// Creates a failed ApiResponse
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static ApiResponse Fail(string message)
    {
        return new ApiResponse { IsSuccess = false, Message = message };
    }

    /// <summary>
    /// Creates a failed ApiResponse with validation errors
    /// </summary>
    /// <param name="errors"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static ApiResponse Fail(IDictionary<string, string[]> errors, string message = "One or more validation errors occurred.")
    {
        return new ApiResponse { IsSuccess = false, Errors = errors, Message = message };
    }
}

/// <summary>
/// Standard API response structure with data
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// The data returned by the API
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful ApiResponse with data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static ApiResponse<T> Success(T data, string? message = null)
    {
        return new ApiResponse<T> { IsSuccess = true, Data = data, Message = message };
    }
}