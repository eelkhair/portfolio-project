using System.Net;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;

namespace UserApi.Features.Account;

/// <summary>
/// Dapr-invoked (future public-api). Creates a Keycloak user and adds them to /Applicants.
/// Same contract as <see cref="SignupAdminEndpoint"/>.
/// </summary>
public class SignupApplicantEndpoint(
    ISignupCommandService signupService,
    ILogger<SignupApplicantEndpoint> logger)
    : Endpoint<SignupRequest, SignupResponse>
{
    private const string GroupPath = "/Applicants";

    public override void Configure()
    {
        Post("account/signup/applicant");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SignupRequest request, CancellationToken ct)
    {
        logger.LogInformation("user-api signup/applicant: {Email}", request.Email);

        var result = await signupService.SignupAsync(
            request.Email, request.FirstName, request.LastName,
            request.Password, GroupPath, ct,
            username: request.Username);

        if (!result.Success || result.Data?.Id is null)
        {
            var status = result.StatusCode == HttpStatusCode.OK
                ? HttpStatusCode.InternalServerError
                : result.StatusCode;
            logger.LogWarning("user-api signup/applicant failed: {Status} {Message}",
                status, result.Exceptions?.Message);
            await Send.StringAsync(result.Exceptions?.Message ?? "Signup failed",
                (int)status, cancellation: ct);
            return;
        }

        await Send.CreatedAtAsync(
            "account/signup/applicant",
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
