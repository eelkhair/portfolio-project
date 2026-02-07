// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System.Net;

namespace JobBoard.AI.API.Helpers;

/// <summary>
/// Represents a standard API response with a generic payload.
/// </summary>
/// <typeparam name="T">The type of data contained in the response.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets the exception details associated with the API response.
    /// </summary>
    /// <remarks>
    /// This property contains information about errors that occurred during processing, including
    /// a message and an optional dictionary of validation errors or additional details.
    /// </remarks>
    public ApiError? Exceptions { get; set; }

    /// <summary>
    /// Gets or sets the data payload of the API response.
    /// </summary>
    /// <remarks>
    /// This property represents the main content of the API response, typically containing the
    /// requested resource or the result of an operation. The type of the data is specified by the
    /// generic parameter <typeparamref name="T"/>.
    /// </remarks>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the API response indicates a successful operation.
    /// </summary>
    /// <remarks>
    /// This property is a boolean flag that represents the success status of the API response. When true, it means the operation was successful.
    /// When false, it denotes that the operation encountered an error or failure.
    /// </remarks>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code associated with the API response.
    /// </summary>
    /// <remarks>
    /// This property indicates the result of the API operation, based on standard HTTP status codes,
    /// such as 200 for success, 400 for client error, or 500 for server error.
    /// </remarks>
    public HttpStatusCode StatusCode { get; set; }
}

/// <summary>
/// Represents an error encountered during API processing, including a message and optional detailed errors.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Gets or sets the error message describing the reason for the failure.
    /// </summary>
    /// <remarks>
    /// This property contains a human-readable message that provides details about the error
    /// encountered during the API request. It is typically used to convey high-level information
    /// about the issue to the client.
    /// </remarks>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets a collection of detailed errors related to the API response.
    /// </summary>
    /// <remarks>
    /// This property provides a dictionary where the keys represent specific fields or parameters,
    /// and the values are arrays of error messages related to those fields or parameters.
    /// </remarks>
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>
/// Provides utility methods for generating standardized API responses.
/// </summary>
public static class ApiResponse
{
    /// <summary>
    /// Creates a successful API response containing the specified data and status code.
    /// </summary>
    /// <typeparam name="T">The type of the data to include in the response.</typeparam>
    /// <param name="data">The data payload to include in the API response.</param>
    /// <param name="status">The HTTP status code for the response. Defaults to 200 (OK).</param>
    /// <returns>A standardized <see cref="ApiResponse{T}"/> object indicating a successful API response with the provided data and status code.</returns>
    public static ApiResponse<T> Success<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
        => new ApiResponse<T>
        {
            Success = true,
            Data = data,
            StatusCode = status,
            Exceptions = null
        };

    /// <summary>
    /// Creates a failed API response containing the specified error message, status code, and optional validation errors.
    /// </summary>
    /// <typeparam name="T">The expected data type of the response payload, typically null in case of failure.</typeparam>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="status">The HTTP status code to associate with the response. Defaults to 400 (BadRequest).</param>
    /// <param name="errors">An optional dictionary of validation errors, where the key is the field name and the value is an array of error messages.</param>
    /// <returns>A standardized <see cref="ApiResponse{T}"/> object indicating a failed API response with the provided error details, status code, and optional validation errors.</returns>
    public static ApiResponse<T> Fail<T>(
        string message,
        HttpStatusCode status = HttpStatusCode.BadRequest,
        Dictionary<string, string[]>? errors = null)
        => new ApiResponse<T>
        {
            Success = false,
            Data = default,
            StatusCode = status,
            Exceptions = new ApiError
            {
                Message = message,
                Errors = errors
            }
        };
}