using System.Diagnostics;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Mcp.Common;

namespace JobBoard.Application.Infrastructure.Decorators;

public class UserContextCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> decorated,
    IActivityFactory activityFactory,
    IUserAccessor userAccessor, IUserSyncService userSyncService)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (request is IAnonymousRequest)
            return await decorated.HandleAsync(request, cancellationToken);

        // Outbox processor: internal system user, skip DB sync and tracing
        if (string.Equals(typeof(TRequest).Name, "ProcessOutboxMessageCommand", StringComparison.Ordinal))
        {
            request.UserId = userAccessor.UserId;
            return await decorated.HandleAsync(request, cancellationToken);
        }

        using (var activity = activityFactory.StartActivity($"{typeof(TRequest).Name}.user_context", ActivityKind.Internal))
        {
            var authenticatedUserId = userAccessor.UserId;
            if (string.IsNullOrEmpty(authenticatedUserId))
                throw new UnauthorizedAccessException("User is not authenticated for this request.");

            await userSyncService.EnsureUserExistsAsync(authenticatedUserId, cancellationToken);
            request.UserId = authenticatedUserId;
        }
        return await decorated.HandleAsync(request, cancellationToken);
    }
}
