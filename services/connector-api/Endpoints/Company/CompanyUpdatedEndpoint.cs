using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Endpoints.Company;

public static class CompanyUpdatedEndpoint
{
    public static WebApplication MapCompanyUpdatedEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/company-updated",
            [Topic("rabbitmq.pubsub", "monolith.company-updated.v1")]
            async (
                EventDto<CompanyUpdatedV1Event> @event,
                UpdateCompanySaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<CompanyUpdatedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("update.company");
                parentSpan?.SetTag("company.uid", @event.Data.CompanyUId);
                parentSpan?.SetTag("event.type", @event.EventType);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"{IdempotencyOptions.Prefix}{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("update.company.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received company updated event {CompanyUId}", @event.Data.CompanyUId);
                    logger.LogDebug("Checking idempotency key {IdempotencyKey}", @event.IdempotencyKey);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping company update. Idempotency key already processed");
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
                    logger.LogError(ex, "Unhandled error while updating company {CompanyUId}", @event.Data.CompanyUId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("update.company.finalize"))
                {
                    logger.LogInformation("Marking update workflow as completed");

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

                logger.LogInformation("Company {CompanyUId} updated successfully", @event.Data.CompanyUId);
                return Results.Accepted();
            });

        return app;
    }
}
