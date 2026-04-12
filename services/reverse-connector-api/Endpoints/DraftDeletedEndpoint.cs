using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr;
using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using JobBoard.IntegrationEvents.Draft;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Models;

namespace ReverseConnectorAPI.Endpoints;

public static class DraftDeletedEndpointExtensions
{
    public static WebApplication MapDraftDeletedEndpoint(this WebApplication app)
    {
        app.MapPost("/sync/draft-deleted",
            [Topic("rabbitmq.pubsub", "micro.draft-deleted.v1")]
            async (
                EventDto<DraftDeletedV1Event> @event,
                MonolithHttpClient monolithClient,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<DraftDeletedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("reverse-sync.draft.delete");
                parentSpan?.SetTag("sync.direction", "reverse");
                parentSpan?.SetTag("sync.entity", "draft");
                parentSpan?.SetTag("draft.uid", @event.Data.UId);
                parentSpan?.SetTag("draft.companyUid", @event.Data.CompanyUId);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"ReverseDraftDeleted:{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("reverse-sync.draft.delete.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received micro draft deleted event {DraftUId}", @event.Data.UId);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey,
                        cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation(
                            "Skipping reverse draft delete sync. Idempotency key already processed");
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
                    await monolithClient.DeleteDraftAsync(
                        @event.Data.UId, @event.Data.CompanyUId, @event.UserId, cancellationToken);
                }
                catch (Exception ex)
                {
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error while reverse-syncing draft delete {DraftUId}",
                        @event.Data.UId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("reverse-sync.draft.delete.finalize"))
                {
                    logger.LogInformation("Marking reverse draft delete sync as completed");

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

                logger.LogInformation("Draft {DraftUId} reverse-delete synced to monolith successfully",
                    @event.Data.UId);
                return Results.Accepted();
            });

        return app;
    }
}
