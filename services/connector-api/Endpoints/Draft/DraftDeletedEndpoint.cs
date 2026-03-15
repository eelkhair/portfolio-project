using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Company;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Draft;

namespace ConnectorAPI.Endpoints.Draft;

public static class DraftDeletedEndpoint
{
    public static WebApplication MapDraftDeletedEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/draft-deleted",
            [Topic("rabbitmq.pubsub", "monolith.draft-deleted.v1")]
            async (
                EventDto<DraftDeletedV1Event> @event,
                DraftSyncSaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<DraftDeletedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("sync.draft.delete");
                parentSpan?.SetTag("draft.uid", @event.Data.UId);
                parentSpan?.SetTag("draft.companyUid", @event.Data.CompanyUId);
                parentSpan?.SetTag("event.type", @event.EventType);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"DraftDeleted:{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("sync.draft.delete.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received draft deleted event {DraftUId}", @event.Data.UId);
                    logger.LogDebug("Checking idempotency key {IdempotencyKey}", @event.IdempotencyKey);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping draft delete sync. Idempotency key already processed");
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
                    await saga.HandleDeleteAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error while syncing draft delete {DraftUId}", @event.Data.UId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("sync.draft.delete.finalize"))
                {
                    logger.LogInformation("Marking draft delete sync as completed");

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

                logger.LogInformation("Draft {DraftUId} delete synced successfully", @event.Data.UId);
                return Results.Accepted();
            });

        return app;
    }
}
