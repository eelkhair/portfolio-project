using System.Diagnostics;
using System.Security.Cryptography;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure.Keycloak;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Account.Signup;

/// <summary>
/// One-click guest signup invoked from the Keycloak login page. Creates a throwaway
/// Keycloak user with randomly generated credentials and adds them to the requested group
/// (<c>/Applicants</c> for the public app, <c>/Admins</c> for the admin app). The user is
/// tagged with attribute <c>anonymous=true</c> so a future cleanup job can purge stale
/// guests.
/// No Turnstile/captcha check — abuse protection lives at the controller layer via rate
/// limiting. The cleartext password is returned in the response so the Keycloak login
/// page JS can type it into <c>#kc-form-login</c> and submit the form normally.
/// </summary>
public class SignupAnonymousCommand : BaseCommand<AnonymousSignupResponseDto>, IAnonymousRequest
{
    /// <summary>Keycloak group path to add the guest to — e.g. <c>/Applicants</c> or <c>/Admins</c>. Set by the controller, not the client.</summary>
    public string GroupPath { get; set; } = string.Empty;

    /// <summary>Remote IP forwarded from the controller for telemetry.</summary>
    public string? RemoteIp { get; set; }
}

public class SignupAnonymousCommandHandler(
    IHandlerContext handlerContext,
    IKeycloakAdminClient keycloak)
    : BaseCommandHandler(handlerContext), IHandler<SignupAnonymousCommand, AnonymousSignupResponseDto>
{
    public async Task<AnonymousSignupResponseDto> HandleAsync(SignupAnonymousCommand request, CancellationToken ct)
    {
        var target = request.GroupPath switch
        {
            "/Admins" => "admin-anonymous",
            "/Applicants" => "public-anonymous",
            _ => "anonymous"
        };
        Activity.Current?.SetTag("signup.target", target);

        if (string.IsNullOrWhiteSpace(request.GroupPath))
        {
            throw new InvalidOperationException("GroupPath is required for anonymous signup.");
        }

        var username = $"guest_{GenerateBase36Token(10)}";
        var password = GenerateUrlSafePassword(24);
        var email = $"{username}@guest.jobboard.local";

        var groupId = await keycloak.FindGroupIdByNameAsync(request.GroupPath, ct)
            ?? throw new InvalidOperationException(
                $"Keycloak group {request.GroupPath} not found. Realm may be misconfigured.");

        var userId = await keycloak.CreateUserWithPasswordAsync(
            email, firstName: "Guest", lastName: "Visitor", password, ct,
            username: username,
            attributes: new Dictionary<string, List<string>>
            {
                ["anonymous"] = ["true"]
            });

        await keycloak.AddUserToGroupAsync(userId, groupId, ct);

        Logger.LogInformation(
            "Anonymous signup: {Username} -> {UserId} added to {Group}",
            username, userId, request.GroupPath);

        return new AnonymousSignupResponseDto
        {
            Username = username,
            Password = password
        };
    }

    private static string GenerateBase36Token(int length)
    {
        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }
        return new string(chars);
    }

    private static string GenerateUrlSafePassword(int length)
    {
        // URL-safe base64: drop padding and swap +/= → -_ so the value is safe to ship
        // in JSON and paste into a plain <input type="password"> without escaping.
        var byteLen = (int)Math.Ceiling(length * 3d / 4d);
        Span<byte> bytes = stackalloc byte[byteLen];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=')[..length];
    }
}
