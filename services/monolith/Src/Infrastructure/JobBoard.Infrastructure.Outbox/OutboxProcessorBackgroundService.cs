using JobBoard.Application.Actions.Outbox;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Outbox;

public sealed class OutboxProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processor background service started (interval: {Interval}s)", Interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
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

                await handler.HandleAsync(new ProcessOutboxMessageCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("Outbox processor background service stopped");
    }
}
