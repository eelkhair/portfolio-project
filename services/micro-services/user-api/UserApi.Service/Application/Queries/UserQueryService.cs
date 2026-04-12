using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserApi.Application.Queries.Interfaces;
using UserAPI.Contracts.Models.Responses;
using UserApi.Infrastructure.Data;

namespace UserApi.Application.Queries;

public partial class UserQueryService(IUserDbContext dbContext, ILogger<UserQueryService> logger) : IUserQueryService
{
    public async Task<List<UserResponse>> ListAsync(CancellationToken ct)
    {
        LogFetchingUsers(logger);

        var users = await dbContext.Users.AsNoTracking().ToListAsync(ct);

        var response = users.Select(u => new UserResponse
        {
            UId = u.UId,
            Email = u.Email,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();

        LogUsersFetched(logger, response.Count);
        return response;
    }

    [LoggerMessage(LogLevel.Information, "Fetching all users")]
    static partial void LogFetchingUsers(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Users fetched, returned {Count} users")]
    static partial void LogUsersFetched(ILogger logger, int count);
}
