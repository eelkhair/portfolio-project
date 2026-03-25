using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AdminApi.Tests.Helpers;

/// <summary>
/// Helper to set up DaprClient mock responses for InvokeMethodWithResponseAsync.
/// Also stubs CreateInvokeMethodRequest to return a real HttpRequestMessage
/// since the mocked DaprClient returns null by default.
/// </summary>
public static class DaprClientMockExtensions
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Stubs CreateInvokeMethodRequest to return a real HttpRequestMessage.
    /// Must be called for any service that uses CreateInvokeMethodRequest.
    /// </summary>
    public static void SetupCreateInvokeMethodRequest(this DaprClient client)
    {
        client.CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var method = callInfo.ArgAt<HttpMethod>(0);
                var appId = callInfo.ArgAt<string>(1);
                var methodName = callInfo.ArgAt<string>(2);
                return new HttpRequestMessage(method, new Uri($"http://{appId}/{methodName}"));
            });
    }

    /// <summary>
    /// Configures InvokeMethodWithResponseAsync to return a successful JSON response.
    /// </summary>
    public static void SetupInvokeMethodWithResponse<T>(
        this DaprClient client,
        T responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(responseBody, JsonOpts);
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        client.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);
    }

    /// <summary>
    /// Configures InvokeMethodWithResponseAsync to return a failure response.
    /// </summary>
    public static void SetupInvokeMethodWithErrorResponse(
        this DaprClient client,
        HttpStatusCode statusCode,
        string errorMessage = "Error")
    {
        var json = JsonSerializer.Serialize(new { message = errorMessage }, JsonOpts);
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        client.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);
    }

    /// <summary>
    /// Configures InvokeMethodWithResponseAsync to throw an exception.
    /// </summary>
    public static void SetupInvokeMethodWithException(
        this DaprClient client,
        Exception exception)
    {
        client.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
    }
}
