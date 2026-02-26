using JobBoard.Infrastructure.Diagnostics.Observability;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class TracingFiltersTests
{
    [Theory]
    [InlineData("/api/companies", true)]
    [InlineData("/api/jobs", true)]
    [InlineData("/odata/companies", true)]
    [InlineData("/healthchecks-ui", false)]
    [InlineData("/healthchecks", false)]
    [InlineData("/health", false)]
    [InlineData("/api/health", false)]
    [InlineData("/scalar/v1", false)]
    [InlineData("/swagger/index.html", false)]
    [InlineData("/openapi/v1.json", false)]
    [InlineData("/v2/track", false)]
    [InlineData("/health-results", false)]
    [InlineData("/ui/resources/some.js", false)]
    public void AspNetCoreFilter_ShouldFilterPathsCorrectly(string path, bool expected)
    {
        var options = new AspNetCoreTraceInstrumentationOptions();
        options.AddFilters();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        var result = options.Filter!(httpContext);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("https://api.example.com/drafts/123", true)]
    [InlineData("https://api.example.com/companies", true)]
    [InlineData("https://config.azconfig.io/keys", false)]
    [InlineData("https://api.example.com/.well-known/openid-configuration", false)]
    [InlineData("https://api.example.com/discovery/v2.0/keys", false)]
    [InlineData("https://api.example.com/api/health", false)]
    [InlineData("https://api.example.com/scalar/docs", false)]
    [InlineData("https://api.example.com/discovery/keys", false)]
    [InlineData("https://api.example.com/cfg-omni/test", false)]
    [InlineData("https://api.example.com/health-results", false)]
    [InlineData("https://api.example.com/QuickPulseService/ping", false)]
    public void HttpClientFilter_ShouldFilterRequestsCorrectly(string url, bool expected)
    {
        var options = new HttpClientTraceInstrumentationOptions();
        options.AddFilters();

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        var result = options.FilterHttpRequestMessage!(request);

        result.ShouldBe(expected);
    }

    [Fact]
    public void HttpClientFilter_WithDaprGrpcPath_ShouldFilter()
    {
        var options = new HttpClientTraceInstrumentationOptions();
        options.AddFilters();

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://localhost:50001/dapr.proto.runtime.v1.Dapr/GetConfiguration");

        var result = options.FilterHttpRequestMessage!(request);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Source_ShouldBeNamedJobBoard()
    {
        TracingFilters.Source.Name.ShouldBe("JobBoard");
    }
}
