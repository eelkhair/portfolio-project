using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Dapr.Client;
using Microsoft.AspNetCore.Http;

namespace Elkhair.Dev.Common.Application;

public static class DaprExtensions
{
    public static HttpClient GetHttpClient(string appId, HttpContext httpContext)
    {
        var client = new DaprClientBuilder().Build();
        var httpClient = client.CreateInvokableHttpClient(appId);
        var token = httpContext.Request.Headers["Authorization"].ToString() ?? string.Empty;
        
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", string.Empty));
        
        return httpClient;
    }
    
    public static async Task<ApiResponse<T>> Process<T>(Func<Task<T>> func)
    {
        try
        {
            var response = await func().ConfigureAwait(false);
            return new ApiResponse<T>()
            {
                StatusCode = HttpStatusCode.OK,
                Data = response,
                Success = true
            };
        }
        catch (InvocationException e)
        {
            var errorText = await e.Response.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<ApiError>(errorText);
            return new ApiResponse<T>()
            {
                StatusCode = e.Response.StatusCode,
                Exceptions = error,
                Success = false
            };
        }
    }
    
    public static ClaimsPrincipal CreateUser(string userId)
    {
        var claims = new List<Claim> () { 
            new Claim (ClaimTypes.NameIdentifier, userId), 
        }; 
        var identity = new ClaimsIdentity (claims, "TestAuthType"); 
        return new ClaimsPrincipal (identity);
    }
    
}