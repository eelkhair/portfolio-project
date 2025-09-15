using Auth0.ManagementApi.Models;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Events;
using UserApi.Infrastructure.Auth0.Interfaces;

namespace UserApi.Application.Commands;

public class UserCommandService(IAuth0Factory factory, IMessageSender sender): IUserCommandService
{
    public async Task ProvisionUserAsync(ProvisionUserEvent user,
        CancellationToken ct)
    {
        var organization = await CreateOrganizationAsync(user, ct);
        if (!organization.Success)
        {
            return;
        }
        var auth0User = await CreateUserAsync(user, ct);
        if (!auth0User.Success)
        {
            return;
        }
        
        var response = await AddMemberToOrganizationAsync(organization.Data?.Id!, auth0User.Data?.UserId!, "rol_jrY03i0FY002L8sQ", ct);
        if (response.Success)
        {
            return;
        }
        
    }

    private async Task<ApiResponse<bool>> AddMemberToOrganizationAsync(string organizationId, string userId, string role, CancellationToken ct)
    {
        var resource = await factory.GetAuth0ResourceAsync(ct);
        return await resource.AddMemberToOrganizationAsync(organizationId, userId, role, ct);
    }

    private async Task<ApiResponse<User>> CreateUserAsync(ProvisionUserEvent user, CancellationToken ct)
    {
        var auth0User = new User
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = DateTime.UtcNow,
        };

        var resource = await factory.GetAuth0ResourceAsync(ct);
        
        return await resource.CreateUserAsync(auth0User, ct);
    }


    private async Task<ApiResponse<Organization>> CreateOrganizationAsync(ProvisionUserEvent user, CancellationToken ct)
    {
        var resource = await factory.GetAuth0ResourceAsync(ct);
        var organization = await resource.CreateOrganizationAsync(user.CompanyUId, user.CompanyName, ct);
        return organization;
    }
    
    
}