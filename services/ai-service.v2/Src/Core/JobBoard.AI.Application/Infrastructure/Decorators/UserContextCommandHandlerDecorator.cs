using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;

namespace JobBoard.AI.Application.Infrastructure.Decorators;

public class UserContextCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> decorated,
    IActivityFactory activityFactory,
    IUserAccessor userAccessor)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        using (var activity = activityFactory.StartActivity(
                   $"{typeof(TRequest).Name}.user_context",
                   ActivityKind.Internal))
        {
            var authenticatedUserId = userAccessor.UserId;
            if (string.IsNullOrEmpty(authenticatedUserId))
            {
                if (request is ISystemCommand)
                {
                    request.UserId = authenticatedUserId ?? string.Empty;
                    return await decorated.HandleAsync(request, cancellationToken);
                }

                throw new UnauthorizedAccessException("User is not authenticated for this request.");
            }
            request.UserId = authenticatedUserId;
        }
        return await decorated.HandleAsync(request, cancellationToken);
    }
}