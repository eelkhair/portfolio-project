using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr;
using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using JobBoard.IntegrationEvents.Company;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Mappers;
using ReverseConnectorAPI.Models;

namespace ReverseConnectorAPI.Endpoints;

public static class CompanyUpdatedEndpointExtensions
{
    public static WebApplication MapCompanyUpdatedEndpoint(this WebApplication app)
    {
        app.MapPost("/sync/company-updated",
            [Topic("rabbitmq.pubsub", "micro.company-updated.v1")]
        async (
                EventDto<MicroCompanyUpdatedV1Event> @event,
                MonolithHttpClient monolithClient,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<MicroCompanyUpdatedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("reverse-sync.company.update");
                parentSpan?.SetTag("sync.direction", "reverse");
                parentSpan?.SetTag("sync.entity", "company");
                parentSpan?.SetTag("company.uid", @event.Data.CompanyUId);
                parentSpan?.SetTag("company.name", @event.Data.Name);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"ReverseCompanyUpdated:{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("reverse-sync.company.update.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received micro company updated event {CompanyUId}", @event.Data.CompanyUId);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey,
                        cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping reverse company update sync. Idempotency key already processed");
                        return Results.Accepted();
                    }

                    spanIdempotency?.SetTag("idempotency.duplicate", false);

                    await client.SaveStateAsync(
                        StateStores.Redis,
                        stateKey,
                        "processing",
                        metadata: new Dictionary<string, string>
(StringComparer.Ordinal)
                        { ["ttlInSeconds"] = IdempotencyOptions.PendingTTLSeconds.ToString() },
                        cancellationToken: cancellationToken);
                }

                try
                {
                    var payload = CompanyMapper.ToUpdatePayload(@event.Data);
                    await monolithClient.SyncCompanyUpdateAsync(payload, @event.UserId, cancellationToken);
                }
                catch (Exception ex)
                {
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error while reverse-syncing company update {CompanyUId}",
                        @event.Data.CompanyUId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("reverse-sync.company.update.finalize"))
                {
                    logger.LogInformation("Marking reverse company update sync as completed");

                    await client.SaveStateAsync(
                        StateStores.Redis,
                        stateKey,
                        "done",
                        metadata: new Dictionary<string, string>
(StringComparer.Ordinal)
                        {
                            ["ttlInSeconds"] = IdempotencyOptions.CompletedTTLSeconds.ToString()
                        },
                        cancellationToken: cancellationToken);
                }

                logger.LogInformation("Company {CompanyUId} update reverse-synced to monolith successfully",
                    @event.Data.CompanyUId);
                return Results.Accepted();
            });

        return app;
    }
}
