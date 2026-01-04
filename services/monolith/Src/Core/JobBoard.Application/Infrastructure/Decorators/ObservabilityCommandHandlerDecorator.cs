using System.Diagnostics;
using FluentValidation;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Infrastructure.Decorators;

public class ObservabilityCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> innerHandler,
    ILogger<TRequest> logger,
    IUserAccessor userAccessor,
    IMetricsService metricsService)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
       
        var requestType = typeof(TRequest).Name;
        Activity.Current?.SetTag("cqrs.request", requestType);
        Activity.Current?.SetTag("userId", userAccessor.UserId);
        Activity.Current?.SetTag("email", userAccessor.Email);

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if(typeof(TRequest).Name != "ProcessOutboxMessageCommand"){
            if (request is BaseCommand<TResult>)
            {
                 logger.LogInformation("Executing command {Request}...", requestType);
            }
            else
            {
                logger.LogInformation("Executing query {Request}...", requestType);
            }
        }
        try
        {
            var result = await innerHandler.HandleAsync(request, cancellationToken);
            metricsService.IncrementCommandSuccess(requestType);
            
            if (typeof(TRequest).Name == "ProcessOutboxMessageCommand") return result;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (request is BaseCommand<TResult>)
            {
                logger.LogInformation("Successfully executed command {Request}", requestType);
            }
            else
            {
                logger.LogInformation("Successfully executed query {Request}", requestType);
            }

            return result;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected failure occurred while executing command {Request}", requestType);
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);

            metricsService.IncrementCommandFailure(requestType);
            throw;
        }
    }
}