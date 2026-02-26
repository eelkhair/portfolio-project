using System.Net;
using JobBoard.Application.Infrastructure.Exceptions;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class ExternalServiceExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var ex = new ExternalServiceException(
            service: "ai-service-v2",
            operation: "drafts.generate",
            statusCode: HttpStatusCode.InternalServerError,
            responseBody: """{"error": "timeout"}""");

        ex.Service.ShouldBe("ai-service-v2");
        ex.Operation.ShouldBe("drafts.generate");
        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void Constructor_ShouldFormatMessageCorrectly()
    {
        var ex = new ExternalServiceException(
            service: "ai-service",
            operation: "settings.get-provider",
            statusCode: HttpStatusCode.NotFound,
            responseBody: "Not Found");

        ex.Message.ShouldBe("ai-service settings.get-provider failed with NotFound: Not Found");
    }

    [Fact]
    public void Constructor_ShouldBeException()
    {
        var ex = new ExternalServiceException(
            service: "test",
            operation: "op",
            statusCode: HttpStatusCode.BadGateway,
            responseBody: "");

        ex.ShouldBeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public void Constructor_ShouldPreserveStatusCode(HttpStatusCode statusCode)
    {
        var ex = new ExternalServiceException("svc", "op", statusCode, "body");

        ex.StatusCode.ShouldBe(statusCode);
    }

    [Fact]
    public void Constructor_WithEmptyResponseBody_ShouldStillFormatMessage()
    {
        var ex = new ExternalServiceException("svc", "op", HttpStatusCode.GatewayTimeout, "");

        ex.Message.ShouldBe("svc op failed with GatewayTimeout: ");
    }
}
