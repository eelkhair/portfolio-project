using System.Net;

namespace UserApi.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode StatusCode, string Content, Uri? LocationHeader)> _responses = new();
    private HttpStatusCode _defaultStatusCode = HttpStatusCode.OK;
    private string _defaultResponseContent = "{}";

    public List<HttpRequestMessage> AllRequests { get; } = [];
    public List<string?> AllRequestBodies { get; } = [];

    public HttpRequestMessage? LastRequest => AllRequests.LastOrDefault();
    public string? LastRequestBody => AllRequestBodies.LastOrDefault();

    public void SetResponse(HttpStatusCode statusCode, string content = "{}")
    {
        _defaultStatusCode = statusCode;
        _defaultResponseContent = content;
    }

    public void EnqueueResponse(HttpStatusCode statusCode, string content = "{}", Uri? locationHeader = null)
    {
        _responses.Enqueue((statusCode, content, locationHeader));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        AllRequests.Add(request);

        if (request.Content != null)
            AllRequestBodies.Add(await request.Content.ReadAsStringAsync(ct));
        else
            AllRequestBodies.Add(null);

        if (_responses.Count > 0)
        {
            var (statusCode, content, locationHeader) = _responses.Dequeue();
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
            if (locationHeader != null)
                response.Headers.Location = locationHeader;
            return response;
        }

        return new HttpResponseMessage(_defaultStatusCode)
        {
            Content = new StringContent(_defaultResponseContent)
        };
    }
}
