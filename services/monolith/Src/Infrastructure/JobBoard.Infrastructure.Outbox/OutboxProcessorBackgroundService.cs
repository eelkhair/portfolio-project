using System.Diagnostics;
using JobBoard.Application.Actions.Outbox;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Mcp.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Outbox;

public sealed class OutboxProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorBackgroundService> logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(500);

        logger.LogInformation("Outbox processor started (interval: {Interval}ms)", delay.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processedAny = false;
            var start = Stopwatch.GetTimestamp();

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var accessor = scope.ServiceProvider.GetRequiredService<IUserAccessor>();
                accessor.UserId = "OutboxProcessor";
                accessor.FirstName = "OutboxProcessor";
                accessor.LastName = "OutboxProcessor";
                accessor.Email = "OutboxProcessor@eelkhair.net";
                accessor.Roles = ["OutboxProcessor"];

                var handler = scope.ServiceProvider
                    .GetRequiredService<IHandler<ProcessOutboxMessageCommand, bool>>();

                processedAny = await handler.HandleAsync(
                    new ProcessOutboxMessageCommand(), 
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            delay = processedAny
                ? TimeSpan.FromMilliseconds(100)
                : TimeSpan.FromSeconds(2);

            var elapsed = Stopwatch.GetElapsedTime(start);

            logger.LogDebug("Outbox iteration complete. ProcessedAny: {ProcessedAny}, Delay: {Delay}ms, Elapsed: {Elapsed}ms",
                processedAny, delay.TotalMilliseconds, elapsed.TotalMilliseconds);

            await Task.Delay(delay, stoppingToken);
        }

        logger.LogInformation("Outbox processor stopped");
    }
}
