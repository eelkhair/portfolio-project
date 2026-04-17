using System.ComponentModel.DataAnnotations;

namespace UserApi.Features.Account;

public class SignupRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(32)]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$")]
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
}

public class SignupResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GroupPath { get; set; } = string.Empty;
}
