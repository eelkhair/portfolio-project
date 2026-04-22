namespace JobBoard.Application.Actions.Account.Signup;

/// <summary>
/// Returned to the Keycloak login page JS after an anonymous guest user is provisioned.
/// The client types these values into the Keycloak login form and submits it, so the
/// password travels back to Keycloak via the normal authenticate endpoint.
/// </summary>
public class AnonymousSignupResponseDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
