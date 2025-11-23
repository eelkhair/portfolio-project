using System.Net;
using System.Runtime.CompilerServices;
using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Elkhair.Dev.Common.Application;
using UserApi.Infrastructure.Auth0.Interfaces;
using ApiError = Elkhair.Dev.Common.Application.ApiError;


namespace UserApi.Infrastructure.Auth0;

public class Auth0Resource(ManagementApiClient client) : IAuth0Resource
{
    public Task<User> GetUserAsync(string userId, CancellationToken ct = default)
        => client.Users.GetAsync(userId, cancellationToken: ct);

    public async Task<ApiResponse<Organization>> CreateOrganizationAsync(Guid uid, string name, CancellationToken ct)
    {
        try
        {
            var organization = await client.Organizations.CreateAsync(new OrganizationCreateRequest
            {
                DisplayName = name,
                Name = uid.ToString(),
                Branding = new OrganizationBranding
                {
                    Colors = new BrandingColors { Primary = "#ffffff", PageBackground = "#222E62" }
                }
              
            }, ct);
            await client.Organizations.CreateConnectionAsync(organization.Id, new OrganizationConnectionCreateRequest()
            {
                AssignMembershipOnLogin = false,
                ConnectionId = "con_h7UaCXoQBfr5XJzq"
            }, ct);	
			
            return CreatedResult(organization);
        }
        catch(Exception e)
        {
            return await HandleException<Organization>(e, [name, uid, ct]);
        }	
    }

    public async Task<ApiResponse<User>> CreateUserAsync(User user, CancellationToken ct)
    {
        try
        {
            var existingUsers = await client.Users.GetUsersByEmailAsync(user.Email, cancellationToken:ct);
            if (existingUsers.Any())
            {
                var metadata =(existingUsers[0].UserMetadata);
                var dict = (Dictionary<string, object>) metadata.ToObject<Dictionary<string, object>>();
               
                var model = (Dictionary<string, string>) user.UserMetadata;

                foreach (var (key, value) in model)
                {
                    dict.Add(key, value);
                }
	
                await client.Users.UpdateAsync(existingUsers[0].UserId,
                    new UserUpdateRequest
                    {
                        UserMetadata= dict
                    }, ct);
                return OkResult(existingUsers[0]);
            }
            var created = await client.Users.CreateAsync(new UserCreateRequest
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Password = "Yamin2025!",
                Connection = "Username-Password-Authentication",
                UserMetadata = user.UserMetadata
            },ct);
            
            return CreatedResult(created);
        }
        catch(Exception e)
        {
            return await HandleException<User>(e, [user, ct]);
        }	
        
    }

    public async Task<ApiResponse<bool>> InviteUserAsync(string organizationId,string email, CancellationToken ct)
    {
        try
        {
            var result = await client.Organizations.CreateInvitationAsync(organizationId,
                new OrganizationCreateInvitationRequest()
                {
                    ClientId = "YXnqj0gOfZJD8Ypje7mdZqdoenCHNzWA",
                    Invitee = new OrganizationInvitationInvitee{Email = email},
                    Inviter = new OrganizationInvitationInviter{ Name = "Elkhair's Job-Board Admin"},
                    SendInvitationEmail = true,
                    Roles = new List<string> { "rol_jrY03i0FY002L8sQ" },
                    ConnectionId = "con_h7UaCXoQBfr5XJzq"
                }, ct);
            return OkResult(true);
        }
        catch(Exception e)
        {
            return await HandleException<bool>(e, [organizationId, email, ct]);
        }	
    }

    public async Task<ApiResponse<bool>> AddMemberToOrganizationAsync(string organizationId, string userId, string role, CancellationToken ct)
    {
        try
        {
            var organizationMember = (await client.Organizations.GetAllMembersAsync(organizationId, new PaginationInfo(), ct))
                .FirstOrDefault(m => m.UserId == userId);
            
            if (organizationMember == null)
            {
                await client.Organizations.AddMembersAsync(organizationId, new OrganizationAddMembersRequest
                {
                    Members = new List<string>() { userId }
                }, ct);
            }

            await client.Organizations.AddMemberRolesAsync(organizationId, userId, new OrganizationAddMemberRolesRequest()
            {
                Roles = new List<string> { "rol_jrY03i0FY002L8sQ" }
            }, ct);				
            
            await client.Users.AssignRolesAsync(userId, new AssignRolesRequest()
            {
                Roles = ["rol_jrY03i0FY002L8sQ"]
            });
            return OkResult(true);
        }
        catch(Exception e)
        {
            return await HandleException<bool>(e, [organizationId, userId, role, ct]);
        }	
    }

    private async Task<ApiResponse<TDto>> HandleException<TDto>(Exception e,  object[]  args = null!, 
        [CallerMemberName] string? caller = null)
    {		
        var brokenRules = new ApiError()
        {
            Errors = new Dictionary<string, string[]>(),
        };
		
        if (e.GetType() == typeof(ErrorApiException)){	
			
            var error = (ErrorApiException) e;			
            brokenRules.Errors.Add(error.StatusCode.ToString(), new[] { e.Message });
			
            return FailedResult<TDto>(brokenRules, error.StatusCode);
        }
		
        if (e.GetType() == typeof(RateLimitApiException)){		
            var error = ((RateLimitApiException) e);
            while (true)
            {
                if (DateTimeOffset.UtcNow > (DateTimeOffset) error.RateLimit?.Reset! )
                {
                    return await (Task<ApiResponse<TDto>>) this.GetType().GetMethod(caller!)!.Invoke(this, args)!;
                }
            }
        }	
		
        brokenRules.Errors.Add("500", new[] { e.Message });
		
        return FailedResult<TDto>(brokenRules,HttpStatusCode.InternalServerError);
    }

    private ApiResponse<TDto> OkResult<TDto>(TDto dto) => new() { Data = dto, StatusCode = HttpStatusCode.OK, Success = true };
    private ApiResponse<TDto> CreatedResult<TDto>(TDto dto) => new ApiResponse<TDto> { Data = dto, StatusCode = HttpStatusCode.Created, Success = true };
    private ApiResponse<TDto> FailedResult<TDto>(ApiError brokenRules, HttpStatusCode statusCode) => new() { Exceptions = brokenRules, StatusCode = statusCode, Success = false };

}