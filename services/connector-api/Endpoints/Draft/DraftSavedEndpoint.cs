using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Company;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Draft;

namespace ConnectorAPI.Endpoints.Draft;

public static class DraftSavedEndpoint
{
    public static WebApplication MapDraftSavedEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/draft-saved",
            [Topic("rabbitmq.pubsub", "monolith.draft-saved.v1")]
            async (
                EventDto<DraftSavedV1Event> @event,
                DraftSyncSaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<DraftSavedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("sync.draft.save");
                parentSpan?.SetTag("sync.direction", "forward");
                parentSpan?.SetTag("sync.entity", "draft");
                parentSpan?.SetTag("draft.uid", @event.Data.UId);
                parentSpan?.SetTag("draft.companyUid", @event.Data.CompanyUId);
                parentSpan?.SetTag("event.type", @event.EventType);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"DraftSaved:{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("sync.draft.save.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received draft saved event {DraftUId}", @event.Data.UId);
                    logger.LogDebug("Checking idempotency key {IdempotencyKey}", @event.IdempotencyKey);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping draft save sync. Idempotency key already processed");
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
                    await saga.HandleSaveAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error while syncing draft save {DraftUId}", @event.Data.UId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("sync.draft.save.finalize"))
                {
                    logger.LogInformation("Marking draft save sync as completed");

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

                logger.LogInformation("Draft {DraftUId} save synced successfully", @event.Data.UId);
                return Results.Accepted();
            });

        return app;
    }
}
