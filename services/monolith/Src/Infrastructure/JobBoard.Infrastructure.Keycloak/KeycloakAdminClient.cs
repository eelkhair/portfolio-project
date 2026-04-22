using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JobBoard.Application.Interfaces.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Keycloak;

/// <summary>
/// Implementation of <see cref="IKeycloakAdminClient"/>. Each method acquires a cached service-account
/// access token via <see cref="IKeycloakTokenProvider"/> and hits the Keycloak Admin REST API.
/// </summary>
public class KeycloakAdminClient(
    IHttpClientFactory httpClientFactory,
    IKeycloakTokenProvider tokenProvider,
    IConfiguration configuration,
    ILogger<KeycloakAdminClient> logger) : IKeycloakAdminClient
{
    private const string HttpClientName = "keycloak-admin";

    public async Task<string?> FindGroupIdByNameAsync(string name, CancellationToken ct)
    {
        var trimmed = name.TrimStart('/');
        var http = await CreateAuthorizedClientAsync(ct);
        var url = $"{GetAdminBaseUrl()}/groups?search={Uri.EscapeDataString(trimmed)}&exact=true";
        var groups = await http.GetFromJsonAsync<List<KeycloakGroupDto>>(url, ct);
        var match = groups?.FirstOrDefault(g => string.Equals(g.Name, trimmed, StringComparison.Ordinal));
        return match?.Id;
    }

    public async Task<string> CreateUserWithPasswordAsync(
        string email, string firstName, string lastName, string password, CancellationToken ct,
        string? username = null,
        IDictionary<string, List<string>>? attributes = null)
    {
        var http = await CreateAuthorizedClientAsync(ct);
        var resolvedUsername = string.IsNullOrWhiteSpace(username) ? email : username.Trim();

        // Pre-check email — gives a predictable 409 surface instead of Keycloak's default error.
        var byEmail = await http.GetFromJsonAsync<List<KeycloakUserDto>>(
            $"{GetAdminBaseUrl()}/users?email={Uri.EscapeDataString(email)}&exact=true", ct);
        if (byEmail is { Count: > 0 })
        {
            throw new KeycloakOperationException(
                "A user with this email already exists.", HttpStatusCode.Conflict);
        }

        // Also pre-check username when it differs from email (public signup case).
        if (!string.Equals(resolvedUsername, email, StringComparison.OrdinalIgnoreCase))
        {
            var byUsername = await http.GetFromJsonAsync<List<KeycloakUserDto>>(
                $"{GetAdminBaseUrl()}/users?username={Uri.EscapeDataString(resolvedUsername)}&exact=true", ct);
            if (byUsername is { Count: > 0 })
            {
                throw new KeycloakOperationException(
                    "A user with this username already exists.", HttpStatusCode.Conflict);
            }
        }

        var payload = new KeycloakUserDto
        {
            Username = resolvedUsername,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Enabled = true,
            EmailVerified = false,
            Credentials =
            [
                new KeycloakCredentialDto { Type = "password", Value = password, Temporary = false }
            ],
            Attributes = attributes is { Count: > 0 }
                ? attributes.ToDictionary(kv => kv.Key, kv => kv.Value)
                : null
        };

        using var res = await http.PostAsJsonAsync($"{GetAdminBaseUrl()}/users", payload, ct);

        if (res.StatusCode == HttpStatusCode.Conflict)
        {
            throw new KeycloakOperationException(
                "A user with this email already exists.", HttpStatusCode.Conflict);
        }
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            logger.LogError("Keycloak create user failed: {Status} {Body}", res.StatusCode, body);
            throw new KeycloakOperationException(
                $"Keycloak create user failed: {res.StatusCode}", res.StatusCode);
        }

        var location = res.Headers.Location?.ToString()
            ?? throw new KeycloakOperationException(
                "Keycloak did not return a Location header.", HttpStatusCode.InternalServerError);
        return location.Split('/').Last();
    }

    public async Task AddUserToGroupAsync(string userId, string groupId, CancellationToken ct)
    {
        var http = await CreateAuthorizedClientAsync(ct);
        using var res = await http.PutAsync(
            $"{GetAdminBaseUrl()}/users/{userId}/groups/{groupId}", new StringContent(""), ct);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            logger.LogError("Keycloak add-to-group failed: {Status} {Body}", res.StatusCode, body);
            throw new KeycloakOperationException(
                $"Keycloak add-to-group failed: {res.StatusCode}", res.StatusCode);
        }
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(CancellationToken ct)
    {
        var token = await tokenProvider.GetAccessTokenAsync(ct);
        var http = httpClientFactory.CreateClient(HttpClientName);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    private string GetAdminBaseUrl()
    {
        var authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Missing Keycloak:Authority configuration.");
        return authority.Replace("/realms/", "/admin/realms/", StringComparison.Ordinal).TrimEnd('/');
    }
}
