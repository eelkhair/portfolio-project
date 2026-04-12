using FastEndpoints;
using UserApi.Application.Queries.Interfaces;
using UserAPI.Contracts.Models.Responses;

namespace UserApi.Features.Users.List;

public class ListUsersEndpoint(IUserQueryService userQueryService) : EndpointWithoutRequest<List<UserResponse>>
{
    public override void Configure()
    {
        Get("/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await userQueryService.ListAsync(ct);
        await Send.OkAsync(users, cancellation: ct);
    }
}
