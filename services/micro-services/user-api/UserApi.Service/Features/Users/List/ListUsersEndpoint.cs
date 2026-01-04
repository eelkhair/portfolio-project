using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UserAPI.Contracts.Models.Responses;
using UserApi.Features.Users.Mappers;
using UserApi.Infrastructure.Data;

namespace UserApi.Features.Users.List;

public class ListUsersEndpoint(IUserDbContext dbContext) : EndpointWithoutRequest<List<UserResponse>, UserMapper>
{
    public override void Configure()
    {
        Get("/users");
        Permissions("read:users");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companies = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken: ct);
        await Send.OkAsync(  companies.Select(Map.FromEntity).ToList(), cancellation: ct);
    }
}
