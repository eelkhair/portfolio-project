using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Endpoints.Company;

public static class CompanyCreatedEndpoint
{
    public static WebApplication MapCompanyCreatedEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/company",
            [Topic("rabbitmq.pubsub", "outbox-events")]
            async (
                EventDto<CompanyCreatedV1Event> @event,
               CompanyProvisioningSaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<CompanyCreatedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                var stateKey = $"{IdempotencyOptions.Prefix}{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("provision.company.idempotency"))
                {
                    logger.LogInformation("Received company created event {CompanyUId}", @event.Data.CompanyUId);
                    logger.LogDebug("Checking idempotency key {IdempotencyKey}", @event.IdempotencyKey);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        logger.LogInformation("Skipping company provisioning. Idempotency key already processed");
                        return Results.Accepted();
                    }

                    await client.SaveStateAsync(
                        StateStores.Redis,
                        stateKey,
                        "processing",
                        metadata: new Dictionary<string, string> { ["ttlInSeconds"] = IdempotencyOptions.PendingTTLSeconds.ToString() },
                        cancellationToken: cancellationToken);
                }

                

                try
                {
                    await saga.HandleAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    using var spanError =
                        activitySource.StartActivity("provision.company.error");

                    spanError?.SetTag("exception", true);
                    spanError?.SetTag("exception.message", ex.Message);
                    logger.LogError(ex, "Unhandled error while provisioning company");
                }
                using (activitySource.StartActivity("provision.company.finalize"))
                {
                    logger.LogInformation("Marking provisioning workflow as completed");

                    await client.SaveStateAsync(
                        StateStores.Redis,
                        stateKey,
                        "done",
                        metadata: new Dictionary<string, string>
                        {
                            ["ttlInSeconds"] = IdempotencyOptions.CompletedTTLSeconds.ToString()
                        },
                        cancellationToken: cancellationToken);
                }

                logger.LogInformation("Company {CompanyUId} provisioned successfully", @event.Data.CompanyUId);
                return Results.Accepted();
            });

        return app;
    }
}

internal static class IdempotencyOptions
{
    public const string Prefix = "Provisioned:";
    public const int PendingTTLSeconds = 120;
    public const int CompletedTTLSeconds = 7 * 24 * 3600;
}
