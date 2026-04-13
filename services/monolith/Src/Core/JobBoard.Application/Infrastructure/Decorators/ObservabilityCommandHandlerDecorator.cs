using System.Diagnostics;
using FluentValidation;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Mcp.Common;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Infrastructure.Decorators;

public partial class ObservabilityCommandHandlerDecorator<TRequest, TResult>(
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
        if (!string.Equals(typeof(TRequest).Name, "ProcessOutboxMessageCommand", StringComparison.Ordinal))
        {
            if (request is BaseCommand<TResult>)
            {
                LogExecutingCommandRequest(requestType);
            }
            else
            {
                LogExecutingQueryRequest(requestType);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await innerHandler.HandleAsync(request, cancellationToken);
            sw.Stop();
            metricsService.IncrementCommandSuccess(requestType);
            metricsService.RecordCommandDuration(requestType, sw.Elapsed.TotalMilliseconds);

            if (string.Equals(typeof(TRequest).Name, "ProcessOutboxMessageCommand", StringComparison.Ordinal)) return result;
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
            sw.Stop();
            metricsService.RecordCommandDuration(requestType, sw.Elapsed.TotalMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "An unexpected failure occurred while executing command {Request}", requestType);
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);

            metricsService.IncrementCommandFailure(requestType);
            metricsService.RecordCommandDuration(requestType, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    [LoggerMessage(LogLevel.Information, "Executing command {Request}...")]
    partial void LogExecutingCommandRequest(string request);

    [LoggerMessage(LogLevel.Information, "Executing query {Request}...")]
    partial void LogExecutingQueryRequest(string request);
}
