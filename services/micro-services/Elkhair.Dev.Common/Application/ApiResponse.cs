using System.Net;

namespace Elkhair.Dev.Common.Application;

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
    public  Dictionary<string, string[]>? Errors { get; set; }
}