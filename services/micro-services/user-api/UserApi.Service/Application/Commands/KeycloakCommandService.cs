using System.Diagnostics;
using System.Net;
using UserApi.Application.Commands.Interfaces;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;
using UserAPI.Contracts.Models.Events;

namespace UserApi.Application.Commands;

public partial class KeycloakCommandService(ActivitySource activitySource, IKeycloakFactory factory, ILogger<KeycloakCommandService> logger) : IKeycloakCommandService
{
    private IKeycloakResource? _resource;

    public async Task<(KeycloakUser User, KeycloakGroup Group)> ProvisionUserAsync(
        ProvisionUserEvent user, CancellationToken ct)
    {
        LogProvisioningUser(logger, user.Email, user.CompanyName);

        _resource ??= await factory.GetKeycloakResourceAsync(ct);

        // 1. Create company group under /Companies/{uid}
        using var activity = activitySource.StartActivity("Creating Keycloak Company Group.");
        var groupResult = await CreateGroupAsync(user, ct);
        ThrowIfFailed(groupResult, "Error creating company group");

        activity?.SetTag("group.id", groupResult.Data?.Id);
        activity?.SetTag("group.name", groupResult.Data?.Name);

        // 2. Create CompanyAdmins sub-group under company group
        using var activity2 = activitySource.StartActivity("Creating CompanyAdmins Sub-Group.");
        var companyAdminsResult = await _resource.CreateSubGroupAsync(groupResult.Data!.Id!, "CompanyAdmins", ct);
        ThrowIfFailed(companyAdminsResult, "Error creating CompanyAdmins sub-group");

        activity2?.SetTag("companyAdmins.group.id", companyAdminsResult.Data?.Id);

        // 3. Create Recruiters sub-group under company group
        using var activity3 = activitySource.StartActivity("Creating Recruiters Sub-Group.");
        var recruitersResult = await _resource.CreateSubGroupAsync(groupResult.Data!.Id!, "Recruiters", ct);
        ThrowIfFailed(recruitersResult, "Error creating Recruiters sub-group");

        activity3?.SetTag("recruiters.group.id", recruitersResult.Data?.Id);

        // 4. Create user
        using var activity4 = activitySource.StartActivity("Creating Keycloak User.");
        var userResult = await CreateUserAsync(user, ct);
        ThrowIfFailed(userResult, "Error creating user");

        activity4?.SetTag("user.id", userResult.Data?.Id);
        activity4?.SetTag("user.email", userResult.Data?.Email);

        // 5. Add user to CompanyAdmins sub-group
        using var activity5 = activitySource.StartActivity("Adding User to CompanyAdmins Group.");
        var addResult = await _resource.AddUserToGroupAsync(userResult.Data!.Id!, companyAdminsResult.Data!.Id!, ct);
        ThrowIfFailed(addResult, "Error adding user to CompanyAdmins group");

        activity5?.SetTag("companyAdmins.group.id", companyAdminsResult.Data?.Id);
        activity5?.SetTag("user.id", userResult.Data?.Id);

        // 6. Send verification email (only for newly created users, non-blocking)
        if (userResult.StatusCode == HttpStatusCode.Created)
        {
            using var activity6 = activitySource.StartActivity("Sending Verification Email.");
            var emailResult = await _resource.SendVerifyEmailAsync(userResult.Data!.Id!, ct);
            if (!emailResult.Success)
                LogVerificationEmailFailed(logger, user.Email, emailResult.Exceptions?.Message);
            activity6?.SetTag("email.sent", emailResult.Success);
        }

        LogProvisioningCompleted(logger, user.Email);
        return (userResult.Data!, groupResult.Data!);
    }

    private async Task<Elkhair.Dev.Common.Application.ApiResponse<KeycloakUser>> CreateUserAsync(
        ProvisionUserEvent user, CancellationToken ct)
    {
        _resource ??= await factory.GetKeycloakResourceAsync(ct);
        var attributes = new Dictionary<string, List<string>>
(StringComparer.Ordinal)
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

    private void ThrowIfFailed<T>(Elkhair.Dev.Common.Application.ApiResponse<T> result, string context)
    {
        if (result.Success) return;

        var errorDetail = result.Exceptions?.Message
                          ?? result.Exceptions?.Errors?.Values.SelectMany(v => v).FirstOrDefault()
                          ?? "Unknown error";

        LogProvisioningFailed(logger, context, errorDetail, result.StatusCode);

        throw new ArgumentException($"{context}: {errorDetail}");
    }

    [LoggerMessage(LogLevel.Information, "Provisioning user '{Email}' for company '{CompanyName}'")]
    static partial void LogProvisioningUser(ILogger logger, string email, string companyName);

    [LoggerMessage(LogLevel.Information, "Provisioning completed for user '{Email}'")]
    static partial void LogProvisioningCompleted(ILogger logger, string email);

    [LoggerMessage(LogLevel.Warning, "Failed to send verification email to {Email}: {Error}")]
    static partial void LogVerificationEmailFailed(ILogger logger, string email, string? error);

    [LoggerMessage(LogLevel.Error, "Keycloak provisioning failed at '{Context}': {Error} (StatusCode: {StatusCode})")]
    static partial void LogProvisioningFailed(ILogger logger, string context, string error, System.Net.HttpStatusCode? statusCode);

    public async Task TeardownCompanyAsync(Guid companyUId, CancellationToken ct)
    {
        _resource ??= await factory.GetKeycloakResourceAsync(ct);

        using var activity = activitySource.StartActivity("Keycloak.TeardownCompany");
        activity?.SetTag("company.uid", companyUId);

        var companyGroup = await _resource.FindGroupByNameAsync(companyUId.ToString(), ct);
        if (companyGroup is null)
        {
            logger.LogInformation("Keycloak group for {CompanyUId} not found — nothing to tear down", companyUId);
            return;
        }

        // Collect users from CompanyAdmins + Recruiters sub-groups so they can be deleted
        // (synthetic demo admin lives only in CompanyAdmins, but real claims may have created
        // recruiters too; either way these accounts exist solely for this company).
        var subGroups = await _resource.GetSubGroupsAsync(companyGroup.Id!, ct);
        var allMembers = new Dictionary<string, KeycloakUser>(StringComparer.Ordinal);
        foreach (var sub in subGroups)
        {
            if (string.IsNullOrEmpty(sub.Id)) continue;
            foreach (var member in await _resource.GetGroupMembersAsync(sub.Id, ct))
            {
                if (!string.IsNullOrEmpty(member.Id))
                    allMembers[member.Id] = member;
            }
        }

        // Delete the parent group first — Keycloak cascades sub-group membership.
        // Then delete each user we collected (they're company-scoped synthetic accounts,
        // safe to remove since the company itself is gone).
        var deleteGroup = await _resource.DeleteGroupAsync(companyGroup.Id!, ct);
        ThrowIfFailed(deleteGroup, "Error deleting company group");

        foreach (var (id, user) in allMembers)
        {
            var del = await _resource.DeleteUserAsync(id, ct);
            if (!del.Success)
                logger.LogWarning("Failed to delete Keycloak user {Email} ({Id}): {Error}",
                    user.Email, id, del.Exceptions?.Message);
        }

        logger.LogInformation("Tore down Keycloak group {CompanyUId} + {UserCount} users",
            companyUId, allMembers.Count);
    }

    public async Task SwapDemoAdminAsync(
        Guid companyUId,
        string newAdminEmail,
        string newAdminFirstName,
        string newAdminLastName,
        CancellationToken ct)
    {
        _resource ??= await factory.GetKeycloakResourceAsync(ct);

        using var activity = activitySource.StartActivity("Keycloak.SwapDemoAdmin");
        activity?.SetTag("company.uid", companyUId);
        activity?.SetTag("new.admin.email", newAdminEmail);

        var companyGroup = await _resource.FindGroupByNameAsync(companyUId.ToString(), ct)
            ?? throw new InvalidOperationException(
                $"Cannot claim demo company {companyUId}: Keycloak group not found");

        var subGroups = await _resource.GetSubGroupsAsync(companyGroup.Id!, ct);
        var companyAdmins = subGroups.FirstOrDefault(g =>
            string.Equals(g.Name, "CompanyAdmins", StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"CompanyAdmins sub-group missing under {companyUId}");

        // Find or create the real user. The visitor just signed up via Keycloak so
        // FindUserByEmailAsync should resolve them; CreateUserAsync is idempotent and
        // returns the existing user when one matches.
        var existingNewUser = await _resource.FindUserByEmailAsync(newAdminEmail, ct);
        var newUserId = existingNewUser?.Id;
        if (string.IsNullOrEmpty(newUserId))
        {
            var created = await _resource.CreateUserAsync(
                newAdminEmail, newAdminFirstName, newAdminLastName, attributes: null, ct);
            ThrowIfFailed(created, "Error creating real admin user during claim");
            newUserId = created.Data!.Id;
        }

        // Add the real user to CompanyAdmins.
        var addResult = await _resource.AddUserToGroupAsync(newUserId!, companyAdmins.Id!, ct);
        ThrowIfFailed(addResult, "Error adding real admin to CompanyAdmins");

        // Pluck the synthetic demo admin (email pattern: demo-{guid}@demo.elkhair.tech)
        // out of the group, then delete the synthetic user account entirely.
        var members = await _resource.GetGroupMembersAsync(companyAdmins.Id!, ct);
        foreach (var m in members.Where(m =>
            m.Email != null && m.Email.EndsWith("@demo.elkhair.tech", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrEmpty(m.Id)) continue;
            await _resource.RemoveUserFromGroupAsync(m.Id, companyAdmins.Id!, ct);
            await _resource.DeleteUserAsync(m.Id, ct);
        }

        logger.LogInformation("Swapped synthetic demo admin → {NewAdminEmail} on company {CompanyUId}",
            newAdminEmail, companyUId);
    }
}
