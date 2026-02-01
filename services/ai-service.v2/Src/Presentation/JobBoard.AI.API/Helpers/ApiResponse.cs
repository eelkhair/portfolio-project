using System.Net;

namespace JobBoard.AI.API.Helpers;

public class ApiResponse<T>
{
    public ApiError? Exceptions { get; set; }
    public T? Data { get; set; }
    public bool Success { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}

public class ApiError
{
    public string? Message { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
        => new()
        {
            Success = true,
            Data = data,
            StatusCode = status,
            Exceptions = null
        };

    public static ApiResponse<T> Fail<T>(
        string message,
        HttpStatusCode status = HttpStatusCode.BadRequest,
        Dictionary<string, string[]>? errors = null)
        => new()
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
