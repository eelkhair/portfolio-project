using System.Net;
using Elkhair.Dev.Common.Application;
using UserApi.Infrastructure.Keycloak.Interfaces;
using ApiError = Elkhair.Dev.Common.Application.ApiError;

namespace UserApi.Infrastructure.Keycloak;

/// <summary>
/// Implements Keycloak Admin REST API operations for user and group provisioning.
/// </summary>
public class KeycloakResource(HttpClient http, string adminApiBaseUrl) : IKeycloakResource
{
    private readonly string _baseUrl = adminApiBaseUrl.TrimEnd('/');

    public async Task<ApiResponse<KeycloakGroup>> CreateGroupAsync(Guid uid, string name, CancellationToken ct)
    {
        try
        {
            // Find the "Companies" parent group
            var companiesGroup = await FindGroupByNameAsync("Companies", ct)
                ?? throw new InvalidOperationException(
                    "Parent group 'Companies' not found in Keycloak. Please create it manually.");

            // Create company group as sub-group of Companies
            var companyGroupName = uid.ToString();
            var result = await CreateSubGroupAsync(companiesGroup.Id!, companyGroupName, ct);

            return result;
        }
        catch (Exception e)
        {
            return FailedResult<KeycloakGroup>(e);
        }
    }

    public async Task<ApiResponse<KeycloakGroup>> CreateSubGroupAsync(
        string parentGroupId, string name, CancellationToken ct)
    {
        try
        {
            var body = new { name };
            using var response = await http.PostAsJsonAsync(
                $"{_baseUrl}/groups/{parentGroupId}/children", body, ct);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                // Sub-group already exists — find and return it
                var children = await GetSubGroupsAsync(parentGroupId, ct);
                var existing = children.FirstOrDefault(g => g.Name == name);
                return existing != null
                    ? OkResult(existing)
                    : throw new InvalidOperationException(
                        $"Group '{name}' reported as existing (409) but not found under parent {parentGroupId}.");
            }

            response.EnsureSuccessStatusCode();

            var groupId = ExtractIdFromLocationHeader(response);
            var group = new KeycloakGroup { Id = groupId, Name = name };
            return CreatedResult(group);
        }
        catch (Exception e)
        {
            return FailedResult<KeycloakGroup>(e);
        }
    }

    public async Task<List<KeycloakGroup>> GetSubGroupsAsync(string parentGroupId, CancellationToken ct)
    {
        return await http.GetFromJsonAsync<List<KeycloakGroup>>(
            $"{_baseUrl}/groups/{parentGroupId}/children", ct) ?? [];
    }

    public async Task<ApiResponse<KeycloakUser>> CreateUserAsync(
        string email, string firstName, string lastName,
        Dictionary<string, List<string>>? attributes, CancellationToken ct)
    {
        try
        {
            // Check if user already exists
            var existingUser = await FindUserByEmailAsync(email, ct);
            if (existingUser != null)
            {
                // Update attributes on existing user if needed
                if (attributes is { Count: > 0 })
                {
                    var merged = existingUser.Attributes ?? new Dictionary<string, List<string>>();
                    foreach (var (key, value) in attributes)
                        merged[key] = value;

                    var updateBody = new { attributes = merged };
                    using var updateRes = await http.PutAsJsonAsync(
                        $"{_baseUrl}/users/{existingUser.Id}", updateBody, ct);
                    updateRes.EnsureSuccessStatusCode();
                }

                return OkResult(existingUser);
            }

            // Create new user
            var newUser = new KeycloakUser
            {
                Username = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Enabled = true,
                EmailVerified = false,
                Credentials = new List<KeycloakCredential>
                {
                    new() { Type = "password", Value = Guid.NewGuid().ToString("N")[..16], Temporary = true }
                },
                Attributes = attributes
            };

            using var response = await http.PostAsJsonAsync($"{_baseUrl}/users", newUser, ct);
            response.EnsureSuccessStatusCode();

            var userId = ExtractIdFromLocationHeader(response);

            return CreatedResult(newUser with { Id = userId });
        }
        catch (Exception e)
        {
            return FailedResult<KeycloakUser>(e);
        }
    }

    public async Task<ApiResponse<bool>> AddUserToGroupAsync(string userId, string groupId, CancellationToken ct)
    {
        try
        {
            using var response = await http.PutAsync(
                $"{_baseUrl}/users/{userId}/groups/{groupId}",
                new StringContent(""), ct);
            response.EnsureSuccessStatusCode();
            return OkResult(true);
        }
        catch (Exception e)
        {
            return FailedResult<bool>(e);
        }
    }

    public async Task<KeycloakGroup?> FindGroupByNameAsync(string name, CancellationToken ct)
    {
        var groups = await http.GetFromJsonAsync<List<KeycloakGroup>>(
            $"{_baseUrl}/groups?search={Uri.EscapeDataString(name)}&exact=true", ct);
        return groups?.FirstOrDefault(g => g.Name == name);
    }

    public async Task<ApiResponse<bool>> SendVerifyEmailAsync(string userId, CancellationToken ct)
    {
        try
        {
            using var response = await http.PutAsync(
                $"{_baseUrl}/users/{userId}/send-verify-email",
                new StringContent(""), ct);
            response.EnsureSuccessStatusCode();
            return OkResult(true);
        }
        catch (Exception e)
        {
            return FailedResult<bool>(e);
        }
    }

    public async Task<KeycloakUser?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        var users = await http.GetFromJsonAsync<List<KeycloakUser>>(
            $"{_baseUrl}/users?email={Uri.EscapeDataString(email)}&exact=true", ct);
        return users?.FirstOrDefault();
    }

    private static string ExtractIdFromLocationHeader(HttpResponseMessage response)
    {
        var location = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(location))
            throw new InvalidOperationException("Keycloak did not return a Location header.");
        return location.Split('/').Last();
    }

    private static ApiResponse<TDto> OkResult<TDto>(TDto dto) =>
        new() { Data = dto, StatusCode = HttpStatusCode.OK, Success = true };

    private static ApiResponse<TDto> CreatedResult<TDto>(TDto dto) =>
        new() { Data = dto, StatusCode = HttpStatusCode.Created, Success = true };

    private static ApiResponse<TDto> FailedResult<TDto>(Exception e)
    {
        var brokenRules = new ApiError
        {
            Errors = new Dictionary<string, string[]>
            {
                ["500"] = [e.Message]
            }
        };
        return new ApiResponse<TDto>
        {
            Exceptions = brokenRules,
            StatusCode = HttpStatusCode.InternalServerError,
            Success = false
        };
    }
}
