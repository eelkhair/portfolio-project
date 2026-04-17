using System.Text.Json.Serialization;

namespace JobBoard.Infrastructure.Keycloak;

/// <summary>Internal DTO mapping the Keycloak Admin API user payload.</summary>
internal record KeycloakUserDto
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("username")] public string? Username { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("firstName")] public string? FirstName { get; init; }
    [JsonPropertyName("lastName")] public string? LastName { get; init; }
    [JsonPropertyName("enabled")] public bool Enabled { get; init; } = true;
    [JsonPropertyName("emailVerified")] public bool EmailVerified { get; init; }
    [JsonPropertyName("credentials")] public List<KeycloakCredentialDto>? Credentials { get; init; }
}

internal record KeycloakCredentialDto
{
    [JsonPropertyName("type")] public string Type { get; init; } = "password";
    [JsonPropertyName("value")] public string? Value { get; init; }
    [JsonPropertyName("temporary")] public bool Temporary { get; init; } = true;
}

internal record KeycloakGroupDto
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("path")] public string? Path { get; init; }
}

internal sealed class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
}
