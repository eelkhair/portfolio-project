using System.Diagnostics;
using Auth0.ManagementApi.Models;
using Elkhair.Dev.Common.Application;

using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Events;
using UserApi.Infrastructure.Auth0.Interfaces;

namespace UserApi.Application.Commands;

public class Auth0CommandService(ActivitySource activitySource, IAuth0Factory factory) : IAuth0CommandService
{
    private IAuth0Resource? Resource;

    public async Task<(User, Organization)> ProvisionUserAsync(ProvisionUserEvent user,
        CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("Creating Auth0 Organization.");
        var organization = await CreateOrganizationAsync(user, ct);
        if (!organization.Success)
        {
            throw new ArgumentException(organization.Exceptions?.Message?? "Error creating organization.");
        }
        activity?.SetTag("organization.id", organization.Data?.Id);
        activity?.SetTag("organization.name", organization.Data?.Name);
        
        using var activity2 = activitySource.StartActivity("Creating Auth0 User.");
        var auth0User = await CreateUserAsync(user, ct);
        if (!auth0User.Success)
        {
            throw new ArgumentException(auth0User.Exceptions?.Message?? "Error creating user.");
        }
        activity2?.SetTag("user.id", auth0User.Data?.UserId);
        activity2?.SetTag("user.email", auth0User.Data?.Email);
        
        using var activity3 = activitySource.StartActivity("Adding User to Organization.");
     
        var response = await AddMemberToOrganizationAsync(organization.Data?.Id!, auth0User.Data?.UserId!, "rol_jrY03i0FY002L8sQ", ct);
        if (!response.Success)
            throw new ArgumentException(response.Exceptions?.Message ?? "Error adding user to organization.");
        activity3?.SetTag("organization.id", organization.Data?.Id);
        activity3?.SetTag("user.id", auth0User.Data?.UserId);
        return (auth0User.Data!, organization.Data!);
    }

    private async Task<ApiResponse<bool>> AddMemberToOrganizationAsync(string organizationId, string userId, string role, CancellationToken ct)
    {
        Resource ??= await factory.GetAuth0ResourceAsync(ct);
        return await Resource.AddMemberToOrganizationAsync(organizationId, userId, role, ct);
    }

    private async Task<ApiResponse<User>> CreateUserAsync(ProvisionUserEvent user, CancellationToken ct)
    {
        var auth0User = new User
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = DateTime.UtcNow,
            UserMetadata = new Dictionary<string, string> { { user.CompanyUId.ToString() , "company-admin"}}
        };

        Resource ??=  await factory.GetAuth0ResourceAsync(ct);
        
        return await Resource.CreateUserAsync(auth0User, ct);
    }


    private async Task<ApiResponse<Organization>> CreateOrganizationAsync(ProvisionUserEvent user, CancellationToken ct)
    {
        Resource ??= await factory.GetAuth0ResourceAsync(ct);
        var organization = await Resource.CreateOrganizationAsync(user.CompanyUId, user.CompanyName, ct);
        return organization;
    }
    
    
}