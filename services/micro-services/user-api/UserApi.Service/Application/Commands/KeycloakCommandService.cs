using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using UserApi.Application.Commands.Interfaces;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;
using UserAPI.Contracts.Models.Events;

namespace UserApi.Application.Commands;

public class KeycloakCommandService(ActivitySource activitySource, IKeycloakFactory factory, ILogger<KeycloakCommandService> logger) : IKeycloakCommandService
{
    private IKeycloakResource? _resource;

    public async Task<(KeycloakUser User, KeycloakGroup Group)> ProvisionUserAsync(
        ProvisionUserEvent user, CancellationToken ct)
    {
        _resource ??= await factory.GetKeycloakResourceAsync(ct);

        // 1. Create company group under /Companies/{uid}
        using var activity = activitySource.StartActivity("Creating Keycloak Company Group.");
        var groupResult = await CreateGroupAsync(user, ct);
        if (!groupResult.Success)
            throw new ArgumentException(groupResult.Exceptions?.Message ?? "Error creating company group.");

        activity?.SetTag("group.id", groupResult.Data?.Id);
        activity?.SetTag("group.name", groupResult.Data?.Name);

        // 2. Create CompanyAdmins sub-group under company group
        using var activity2 = activitySource.StartActivity("Creating CompanyAdmins Sub-Group.");
        var companyAdminsResult = await _resource.CreateSubGroupAsync(groupResult.Data!.Id!, "CompanyAdmins", ct);
        if (!companyAdminsResult.Success)
            throw new ArgumentException(companyAdminsResult.Exceptions?.Message ?? "Error creating CompanyAdmins sub-group.");

        activity2?.SetTag("companyAdmins.group.id", companyAdminsResult.Data?.Id);

        // 3. Create Recruiters sub-group under company group
        using var activity3 = activitySource.StartActivity("Creating Recruiters Sub-Group.");
        var recruitersResult = await _resource.CreateSubGroupAsync(groupResult.Data!.Id!, "Recruiters", ct);
        if (!recruitersResult.Success)
            throw new ArgumentException(recruitersResult.Exceptions?.Message ?? "Error creating Recruiters sub-group.");

        activity3?.SetTag("recruiters.group.id", recruitersResult.Data?.Id);

        // 4. Create user
        using var activity4 = activitySource.StartActivity("Creating Keycloak User.");
        var userResult = await CreateUserAsync(user, ct);
        if (!userResult.Success)
            throw new ArgumentException(userResult.Exceptions?.Message ?? "Error creating user.");

        activity4?.SetTag("user.id", userResult.Data?.Id);
        activity4?.SetTag("user.email", userResult.Data?.Email);

        // 5. Add user to CompanyAdmins sub-group
        using var activity5 = activitySource.StartActivity("Adding User to CompanyAdmins Group.");
        var addResult = await _resource.AddUserToGroupAsync(userResult.Data!.Id!, companyAdminsResult.Data!.Id!, ct);
        if (!addResult.Success)
            throw new ArgumentException(addResult.Exceptions?.Message ?? "Error adding user to CompanyAdmins group.");

        activity5?.SetTag("companyAdmins.group.id", companyAdminsResult.Data?.Id);
        activity5?.SetTag("user.id", userResult.Data?.Id);

        // 6. Send verification email (only for newly created users, non-blocking)
        if (userResult.StatusCode == HttpStatusCode.Created)
        {
            using var activity6 = activitySource.StartActivity("Sending Verification Email.");
            var emailResult = await _resource.SendVerifyEmailAsync(userResult.Data!.Id!, ct);
            if (!emailResult.Success)
                logger.LogWarning("Failed to send verification email to {Email}: {Error}",
                    user.Email, emailResult.Exceptions?.Message);
            activity6?.SetTag("email.sent", emailResult.Success);
        }

        return (userResult.Data!, groupResult.Data!);
    }

    private async Task<Elkhair.Dev.Common.Application.ApiResponse<KeycloakUser>> CreateUserAsync(
        ProvisionUserEvent user, CancellationToken ct)
    {
        _resource ??= await factory.GetKeycloakResourceAsync(ct);
        var attributes = new Dictionary<string, List<string>>
        {
            ["companyName"] = [user.CompanyName]
        };
        return await _resource.CreateUserAsync(user.Email, user.FirstName, user.LastName, attributes, ct);
    }

    private async Task<Elkhair.Dev.Common.Application.ApiResponse<KeycloakGroup>> CreateGroupAsync(
        ProvisionUserEvent user, CancellationToken ct)
    {
        _resource ??= await factory.GetKeycloakResourceAsync(ct);
        return await _resource.CreateGroupAsync(user.CompanyUId, user.CompanyName, ct);
    }
}
