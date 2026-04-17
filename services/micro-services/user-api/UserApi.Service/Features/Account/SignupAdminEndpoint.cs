using System.Net;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;

namespace UserApi.Features.Account;

/// <summary>
/// Dapr-invoked by admin-api. Creates a Keycloak user and adds them to /Admins.
/// Anonymous at HTTP level — protected in practice by Dapr's API token (internal cluster network).
/// On success returns <see cref="SignupResponse"/>; on failure sets a non-2xx HTTP status
/// so Dapr's InvocationException surfaces the error back to the caller.
/// </summary>
public class SignupAdminEndpoint(
    ISignupCommandService signupService,
    ILogger<SignupAdminEndpoint> logger)
    : Endpoint<SignupRequest, SignupResponse>
{
    private const string GroupPath = "/Admins";

    public override void Configure()
    {
        Post("account/signup/admin");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SignupRequest request, CancellationToken ct)
    {
        logger.LogInformation("user-api signup/admin: {Email}", request.Email);

        var result = await signupService.SignupAsync(
            request.Email, request.FirstName, request.LastName,
            request.Password, GroupPath, ct,
            username: request.Username);

        if (!result.Success || result.Data?.Id is null)
        {
            var status = result.StatusCode == HttpStatusCode.OK
                ? HttpStatusCode.InternalServerError // OK-but-null shouldn't happen; be defensive
                : result.StatusCode;
            logger.LogWarning("user-api signup/admin failed: {Status} {Message}",
                status, result.Exceptions?.Message);
            await Send.StringAsync(result.Exceptions?.Message ?? "Signup failed",
                (int)status, cancellation: ct);
            return;
        }

        await Send.CreatedAtAsync(
            "account/signup/admin",
            new { userId = result.Data.Id },
            new SignupResponse
            {
                UserId = result.Data.Id,
                Email = request.Email,
                GroupPath = GroupPath
            },
            cancellation: ct);
    }
}
