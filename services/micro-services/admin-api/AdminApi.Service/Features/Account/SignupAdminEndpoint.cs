using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AdminApi.Infrastructure.Turnstile;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Account;

/// <summary>
/// Public-facing admin signup. Verifies a Cloudflare Turnstile token, then Dapr-invokes
/// user-api's <c>account/signup/admin</c> which handles the Keycloak user creation and group assignment.
/// </summary>
public class SignupAdminEndpoint(
    DaprClient daprClient,
    ITurnstileVerifier turnstile,
    IHttpContextAccessor httpContext,
    ILogger<SignupAdminEndpoint> logger)
    : Endpoint<SignupAdminRequest, SignupAdminResponse>
{
    public override void Configure()
    {
        Post("/account/signup/admin");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SignupAdminRequest request, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "signup");
        Activity.Current?.SetTag("signup.target", "admin");

        var ip = httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var ok = await turnstile.VerifyAsync(request.TurnstileToken, ip, ct);
        if (!ok)
        {
            logger.LogWarning("admin-api signup/admin: Turnstile failed for {Email}", request.Email);
            await Send.StringAsync("Captcha verification failed.", (int)HttpStatusCode.Forbidden, cancellation: ct);
            return;
        }

        var forward = new UserApiSignupRequest
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = request.Password
        };

        var msg = daprClient.CreateInvokeMethodRequest(System.Net.Http.HttpMethod.Post, "user-api", "account/signup/admin");
        msg.Content = JsonContent.Create(forward);

        var result = await DaprExtensions.Process(() =>
            daprClient.InvokeMethodAsync<UserApiSignupResponse>(msg, cancellationToken: ct));

        if (!result.Success || result.Data is null)
        {
            var status = result.StatusCode == HttpStatusCode.OK
                ? HttpStatusCode.InternalServerError
                : result.StatusCode;
            logger.LogWarning("admin-api signup/admin forwarded to user-api returned {Status}: {Msg}",
                status, result.Exceptions?.Message);
            await Send.StringAsync(
                result.Exceptions?.Message ?? "Signup failed.",
                (int)status, cancellation: ct);
            return;
        }

        logger.LogInformation("admin-api signup/admin succeeded: {Email} -> {UserId}",
            request.Email, result.Data.UserId);
        await Send.CreatedAtAsync(
            "/account/signup/admin",
            new { userId = result.Data.UserId },
            new SignupAdminResponse
            {
                UserId = result.Data.UserId,
                Email = result.Data.Email,
                GroupPath = result.Data.GroupPath
            },
            cancellation: ct);
    }
}
