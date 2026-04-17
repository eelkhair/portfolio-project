using System.ComponentModel.DataAnnotations;

namespace AdminApi.Features.Account;

public class SignupAdminRequest
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

    /// <summary>Cloudflare Turnstile client-side token; server re-verifies with secret key.</summary>
    [Required]
    public string TurnstileToken { get; set; } = string.Empty;
}

public class SignupAdminResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GroupPath { get; set; } = string.Empty;
}

/// <summary>Shape returned by the user-api signup endpoint (see user-api SignupResponse).</summary>
internal sealed class UserApiSignupResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GroupPath { get; set; } = string.Empty;
}

/// <summary>Payload admin-api forwards to user-api (matches UserApi SignupRequest).</summary>
internal sealed class UserApiSignupRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
