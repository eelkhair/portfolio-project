using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr;
using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using JobBoard.IntegrationEvents.Draft;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Mappers;
using ReverseConnectorAPI.Models;

namespace ReverseConnectorAPI.Endpoints;

public static class DraftSavedEndpointExtensions
{
    public static WebApplication MapDraftSavedEndpoint(this WebApplication app)
    {
        app.MapPost("/sync/draft-saved",
            [Topic("rabbitmq.pubsub", "micro.draft-saved.v1")]
            async (
                EventDto<DraftSavedV1Event> @event,
                MonolithHttpClient monolithClient,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<DraftSavedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("reverse-sync.draft.save");
                parentSpan?.SetTag("draft.uid", @event.Data.UId);
                parentSpan?.SetTag("draft.companyUid", @event.Data.CompanyUId);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"ReverseDraftSaved:{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("reverse-sync.draft.save.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received micro draft saved event {DraftUId}", @event.Data.UId);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey,
                        cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping reverse draft save sync. Idempotency key already processed");
                        return Results.Accepted();
                    }

                    spanIdempotency?.SetTag("idempotency.duplicate", false);

                    await client.SaveStateAsync(
                        StateStores.Redis,
                        stateKey,
                        "processing",
                        metadata: new Dictionary<string, string>
                            { ["ttlInSeconds"] = IdempotencyOptions.PendingTTLSeconds.ToString() },
                        cancellationToken: cancellationToken);
                }

                try
                {
                    var payload = DraftMapper.ToPayload(@event.Data);
                    await monolithClient.SyncDraftAsync(payload, @event.UserId, cancellationToken);
                }
                catch (Exception ex)
                {
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error while reverse-syncing draft save {DraftUId}",
                        @event.Data.UId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("reverse-sync.draft.save.finalize"))
                {
                    logger.LogInformation("Marking reverse draft save sync as completed");

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

                logger.LogInformation("Draft {DraftUId} reverse-synced to monolith successfully",
                    @event.Data.UId);
                return Results.Accepted();
            });

        return app;
    }
}
