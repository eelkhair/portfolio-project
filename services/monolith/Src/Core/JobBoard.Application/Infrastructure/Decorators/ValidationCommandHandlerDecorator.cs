using FluentValidation;
using JobBoard.Application.Interfaces.Configurations;

namespace JobBoard.Application.Infrastructure.Decorators;

public class ValidationCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> decorated,
    IValidator<TRequest>? validator = null)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (validator is null) return await decorated.HandleAsync(request, cancellationToken);
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        return await decorated.HandleAsync(request, cancellationToken);
    }
}