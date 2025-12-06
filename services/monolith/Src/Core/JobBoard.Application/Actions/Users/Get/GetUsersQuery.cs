using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Users.Models;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Users.Get;

public class GetUsersQuery : BaseQuery<IQueryable<UserDto>>;

public class GetUsersQueryHandler(IJobBoardQueryDbContext context, ILogger<GetUsersQueryHandler> logger) 
    : BaseQueryHandler(context, logger)
    , IHandler<GetUsersQuery, IQueryable<UserDto>>
{
    public Task<IQueryable<UserDto>> HandleAsync(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = Context.Users.Select(u => new UserDto
        {
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            ExternalId = u.ExternalId,
            CreatedAt = u.CreatedAt,
            CreatedBy = u.CreatedBy,
            UpdatedAt = u.UpdatedAt,
            UpdatedBy = u.UpdatedBy,
            Id = u.Id
            
        });
        
        return Task.FromResult(users);
    }
}