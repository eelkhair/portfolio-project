using FluentValidation;
using FluentValidation.Results;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain;
using JobBoard.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Application.Infrastructure.Decorators;

public class ExceptionHandlingCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> innerHandler)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await innerHandler.HandleAsync(request, cancellationToken);
        }
        catch (DomainException ex)
        {
            var validationFailures = ex.Errors
                .Select(e => new ValidationFailure(e.Code.Split('.')[0], e.Description))
                .ToList();
            throw new ValidationException(validationFailures);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: AppConstants.UniqueConstraintSqlError })
        {
            const string propertyName = "Name";
            var propertyValue = request.GetType().GetProperty(propertyName)?.GetValue(request) ?? "value";

            var validationError = new ValidationFailure(
                propertyName,
                $"A resource with the value '{propertyValue}' already exists. Please choose a different value.");
            throw new ValidationException([validationError]);
        }
       
    }
}