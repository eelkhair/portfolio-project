using System.Diagnostics;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.IntegrationEvents.Company;
using JobBoard.Mcp.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobBoard.API.BackgroundServices;

/// <summary>
/// Periodically scans for expired demo companies and publishes
/// <see cref="DemoCompanyExpiredV1Event"/> for each. The connector-api's cleanup
/// saga subscribes and tears down the company, child entities, and Keycloak group.
/// </summary>
public sealed class DemoCompanySweeperService(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoCompanySweeperService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Demo company sweeper started. Interval: {Interval}, StartupDelay: {StartupDelay}",
            Interval, StartupDelay);

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo company sweeper iteration failed — will retry next interval");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var activitySource = new ActivitySource("JobBoard.DemoSweeper");
        using var activity = activitySource.StartActivity("demo.sweeper.run", ActivityKind.Internal);

        await using var scope = scopeFactory.CreateAsyncScope();

        var accessor = scope.ServiceProvider.GetRequiredService<IUserAccessor>();
        accessor.UserId = "DemoSweeper";
        accessor.FirstName = "Demo";
        accessor.LastName = "Sweeper";
        accessor.Email = "demo-sweeper@eelkhair.net";
        accessor.Roles = ["DemoSweeper"];

        var context = scope.ServiceProvider.GetRequiredService<IJobBoardDbContext>();
        var queryContext = (IJobBoardQueryDbContext)context;
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

        var now = DateTime.UtcNow;
        var expired = await queryContext.Companies
            .Where(c => c.IsDemo && c.DemoExpiresAt != null && c.DemoExpiresAt < now)
            .Select(c => c.Id)
            .ToListAsync(ct);

        activity?.SetTag("companies.expired", expired.Count);
        activity?.SetTag("companies.deleted", 0);
        activity?.SetTag("errors", 0);

        if (expired.Count == 0)
        {
            logger.LogDebug("Demo sweeper found no expired companies");
            return;
        }

        var errors = 0;
        foreach (var companyUId in expired)
        {
            try
            {
                var evt = new DemoCompanyExpiredV1Event(companyUId)
                {
                    UserId = accessor.UserId
                };
                await publisher.PublishAsync(evt, ct);
            }
            catch (Exception ex)
            {
                errors++;
                logger.LogError(ex, "Failed to publish DemoCompanyExpiredV1Event for {CompanyUId}", companyUId);
            }
        }

        await context.SaveChangesAsync(accessor.UserId, ct);

        activity?.SetTag("companies.deleted", expired.Count - errors);
        activity?.SetTag("errors", errors);

        logger.LogInformation(
            "Demo sweeper iteration complete. Expired: {Expired}, Published: {Published}, Errors: {Errors}",
            expired.Count, expired.Count - errors, errors);
    }
}
