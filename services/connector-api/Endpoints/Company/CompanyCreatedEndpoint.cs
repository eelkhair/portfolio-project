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
            [Topic("rabbitmq.pubsub", "monolith.company-created.v1")]
            async (
                EventDto<CompanyCreatedV1Event> @event,
                CompanyProvisioningSaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<CompanyCreatedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("provision.company");
                parentSpan?.SetTag("company.uid", @event.Data.CompanyUId);
                parentSpan?.SetTag("company.admin.uid", @event.Data.AdminUId);
                parentSpan?.SetTag("event.type", @event.EventType);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"{IdempotencyOptions.Prefix}{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("provision.company.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received company created event {CompanyUId}", @event.Data.CompanyUId);
                    logger.LogDebug("Checking idempotency key {IdempotencyKey}", @event.IdempotencyKey);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping company provisioning. Idempotency key already processed");
                        return Results.Accepted();
                    }

                    spanIdempotency?.SetTag("idempotency.duplicate", false);

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
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error while provisioning company {CompanyUId}", @event.Data.CompanyUId);
                    return Results.Accepted();
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
