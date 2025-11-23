namespace JobBoard.Application.Interfaces.Users;

public interface IUserSyncService
{
    Task EnsureUserExistsAsync(string userId, CancellationToken cancellationToken);
}