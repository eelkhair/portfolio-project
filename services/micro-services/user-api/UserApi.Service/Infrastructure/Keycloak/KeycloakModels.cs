using System.Text.Json.Serialization;

namespace UserApi.Infrastructure.Keycloak;

/// <summary>Keycloak Admin REST API user representation.</summary>
public record KeycloakUser
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; init; }

    [JsonPropertyName("credentials")]
    public List<KeycloakCredential>? Credentials { get; init; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; init; }
}

/// <summary>Keycloak credential representation for user creation.</summary>
public record KeycloakCredential
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "password";

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("temporary")]
    public bool Temporary { get; init; } = true;
}

/// <summary>Keycloak group representation.</summary>
public record KeycloakGroup
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }
}

/// <summary>Internal DTO for Keycloak token endpoint response.</summary>
internal sealed class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
