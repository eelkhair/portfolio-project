using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure.Keycloak;
using JobBoard.Application.Interfaces.Infrastructure.Turnstile;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Account.Signup;

/// <summary>
/// Self-signup for the Angular public app: creates a Keycloak user and adds them to /Applicants.
/// Verifies a Cloudflare Turnstile token first.
/// </summary>
public class SignupPublicCommand : BaseCommand<SignupResponseDto>, IAnonymousRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(32)]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username may only contain letters, digits, and the characters . _ -")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string TurnstileToken { get; set; } = string.Empty;

    public string? RemoteIp { get; set; }
}

public class SignupPublicCommandHandler(
    IHandlerContext handlerContext,
    ITurnstileVerifier turnstile,
    IKeycloakAdminClient keycloak)
    : BaseCommandHandler(handlerContext), IHandler<SignupPublicCommand, SignupResponseDto>
{
    private const string GroupPath = "/Applicants";

    public async Task<SignupResponseDto> HandleAsync(SignupPublicCommand request, CancellationToken ct)
    {
        Activity.Current?.SetTag("signup.target", "public");
        Activity.Current?.SetTag("signup.email", request.Email);

        var captchaOk = await turnstile.VerifyAsync(request.TurnstileToken, request.RemoteIp, ct);
        if (!captchaOk)
        {
            throw new ForbiddenAccessException("Captcha verification failed.");
        }

        var groupId = await keycloak.FindGroupIdByNameAsync(GroupPath, ct)
            ?? throw new InvalidOperationException(
                $"Keycloak group {GroupPath} not found. Realm may be misconfigured.");

        var userId = await keycloak.CreateUserWithPasswordAsync(
            request.Email, request.FirstName, request.LastName, request.Password, ct,
            username: request.Username);

        await keycloak.AddUserToGroupAsync(userId, groupId, ct);

        Logger.LogInformation("Signup public: {Username} ({Email}) -> {UserId} added to {Group}",
            request.Username, request.Email, userId, GroupPath);

        return new SignupResponseDto
        {
            UserId = userId,
            Email = request.Email,
            GroupPath = GroupPath
        };
    }
}
