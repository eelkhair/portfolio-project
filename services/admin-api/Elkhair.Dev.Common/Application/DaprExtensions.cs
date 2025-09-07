using System.Net.Http.Headers;
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
    
    
}