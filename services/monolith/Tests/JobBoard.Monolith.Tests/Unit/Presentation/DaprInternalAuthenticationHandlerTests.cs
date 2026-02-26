using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using JobBoard.API.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Presentation;

[Trait("Category", "Unit")]
public class DaprInternalAuthenticationHandlerTests
{
    private const string SchemeName = "DaprInternal";

    [Fact]
    public async Task HandleAuthenticateAsync_FromLoopbackIPv4_ShouldSucceed()
    {
        var handler = await CreateHandlerAsync(IPAddress.Loopback); // 127.0.0.1

        var result = await handler.AuthenticateAsync();

        result.Succeeded.ShouldBeTrue();
        result.Principal.ShouldNotBeNull();
        result.Principal.Identity!.Name.ShouldBe("DaprCron");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_FromLoopbackIPv6_ShouldSucceed()
    {
        var handler = await CreateHandlerAsync(IPAddress.IPv6Loopback); // ::1

        var result = await handler.AuthenticateAsync();

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_FromExternalIP_ShouldFail()
    {
        var handler = await CreateHandlerAsync(IPAddress.Parse("192.168.1.100"));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.ShouldBeFalse();
        result.Failure!.Message.ShouldContain("Unauthorized IP");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_FromLoopback_ShouldUseSchemeNameAsAuthType()
    {
        var handler = await CreateHandlerAsync(IPAddress.Loopback);

        var result = await handler.AuthenticateAsync();

        result.Principal!.Identity!.AuthenticationType.ShouldBe(SchemeName);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithNullIP_ShouldFail()
    {
        var handler = await CreateHandlerAsync(null);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.ShouldBeFalse();
    }

    private static async Task<DaprInternalAuthenticationHandler> CreateHandlerAsync(IPAddress? remoteIp)
    {
        var options = new AuthenticationSchemeOptions();
        var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Get(SchemeName).Returns(options);

        var loggerFactory = NullLoggerFactory.Instance;
        var encoder = UrlEncoder.Default;
        var timeProvider = TimeProvider.System;

        var handler = new DaprInternalAuthenticationHandler(
            optionsMonitor, loggerFactory, encoder, timeProvider);

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteIp;

        var scheme = new AuthenticationScheme(SchemeName, SchemeName, typeof(DaprInternalAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);

        return handler;
    }
}
