using System.Diagnostics;
using FluentValidation;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Infrastructure.Decorators;

public class ObservabilityCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> innerHandler,
    ILogger<TRequest> logger,
    IMetricsService metricsService)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {

        var requestType = typeof(TRequest).Name;
        Activity.Current?.SetTag("cqrs.request", requestType);

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression

        if (request is BaseCommand<TResult>)
        {
            logger.LogInformation("Executing command {Request}...", requestType);
        }
        else
        {
            logger.LogInformation("Executing query {Request}...", requestType);
        }

        try
        {
            var result = await innerHandler.HandleAsync(request, cancellationToken);
            metricsService.IncrementCommandSuccess(requestType);

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