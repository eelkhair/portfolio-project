using System.Diagnostics;
using FluentValidation;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;

namespace JobBoard.Application.Infrastructure.Decorators;

public class ValidationCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> decorated,
    IActivityFactory activityFactory,
    IValidator<TRequest>? validator = null)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (validator is null) return await decorated.HandleAsync(request, cancellationToken);

        using (var activity = activityFactory.StartActivity(
                   $"{typeof(TRequest).Name}.validate",
                   ActivityKind.Internal))
        {
           var validationResult = await validator.ValidateAsync(request, cancellationToken);
                activity?.SetTag("validation.is_valid", validationResult.IsValid);
                activity?.SetTag("validation.error_count", validationResult.Errors.Count);
                if (!validationResult.IsValid)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    throw new ValidationException(validationResult.Errors);
                }
        }
     
        return await decorated.HandleAsync(request, cancellationToken);
    }
}