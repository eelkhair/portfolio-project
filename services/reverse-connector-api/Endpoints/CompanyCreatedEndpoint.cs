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

public static class CompanyCreatedEndpointExtensions
{
    public static WebApplication MapCompanyCreatedEndpoint(this WebApplication app)
    {
        app.MapPost("/sync/company-created",
            [Topic("rabbitmq.pubsub", "micro.company-created.v1")]
            async (
                EventDto<MicroCompanyCreatedV1Event> @event,
                MonolithHttpClient monolithClient,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<MicroCompanyCreatedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("reverse-sync.company.create");
                parentSpan?.SetTag("company.uid", @event.Data.CompanyUId);
                parentSpan?.SetTag("company.name", @event.Data.Name);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"ReverseCompanyCreated:{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("reverse-sync.company.create.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received micro company created event {CompanyUId}", @event.Data.CompanyUId);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey,
                        cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping reverse company create sync. Idempotency key already processed");
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
                    var payload = CompanyMapper.ToCreatePayload(@event.Data);
                    await monolithClient.SyncCompanyCreateAsync(payload, @event.UserId, cancellationToken);
                }
                catch (Exception ex)
                {
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error while reverse-syncing company create {CompanyUId}",
                        @event.Data.CompanyUId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("reverse-sync.company.create.finalize"))
                {
                    logger.LogInformation("Marking reverse company create sync as completed");

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

                logger.LogInformation("Company {CompanyUId} reverse-synced to monolith successfully",
                    @event.Data.CompanyUId);
                return Results.Accepted();
            });

        return app;
    }
}
