using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
            var error = await e.Response.Content.ReadFromJsonAsync<ApiError>();   
            return new ApiResponse<T>()
            {
                StatusCode = e.Response.StatusCode,
                Exceptions = error,
                Success = false
            };
        }
    }
    public static async Task<TRes?> InvokeWithHeadersAsync<TReq, TRes>(
        this DaprClient dapr, HttpMethod method, string appId, string methodName,
        TReq payload, IDictionary<string,string>? headers, CancellationToken ct = default)
    {
        var req = dapr.CreateInvokeMethodRequest(method, appId, methodName);
        req.Content = JsonContent.Create(payload);
        if (headers != null)
            foreach (var h in headers)
                req.Headers.TryAddWithoutValidation(h.Key, h.Value);

        using var res = await dapr.InvokeMethodWithResponseAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TRes>(cancellationToken: ct);
    }
    
}