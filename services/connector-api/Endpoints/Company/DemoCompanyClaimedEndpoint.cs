using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Endpoints.Company;

public static class DemoCompanyClaimedEndpoint
{
    public static WebApplication MapDemoCompanyClaimedEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/demo-company/claimed",
            [Topic("rabbitmq.pubsub", "monolith.demo-company-claimed.v1")]
            async (
                EventDto<DemoCompanyClaimedV1Event> @event,
                DemoCompanyClaimedSaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<DemoCompanyClaimedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("demo-company.claimed");
                parentSpan?.SetTag("sync.direction", "forward");
                parentSpan?.SetTag("sync.entity", "demo-company");
                parentSpan?.SetTag("company.uid", @event.Data.CompanyUId);
                parentSpan?.SetTag("new.admin.uid", @event.Data.NewAdminUserUId);
                parentSpan?.SetTag("event.type", @event.EventType);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);

                var stateKey = $"DemoClaimed:{@event.IdempotencyKey}";
                var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);
                if (existing is not null)
                {
                    logger.LogInformation("Skipping demo claim — idempotency key already processed: {Key}", stateKey);
                    return Results.Accepted();
                }

                await client.SaveStateAsync(StateStores.Redis, stateKey, "processing",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["ttlInSeconds"] = "120" },
                    cancellationToken: cancellationToken);

                try
                {
                    await saga.HandleAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    parentSpan?.AddException(ex);
                    parentSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "Unhandled error during demo company claim {CompanyUId}", @event.Data.CompanyUId);
                    // Return 500 here — claim must succeed atomically; surface the failure
                    // so the visitor's claim flow gets a real error and can retry.
                    return Results.Problem("Claim saga failed", statusCode: 500);
                }

                await client.SaveStateAsync(StateStores.Redis, stateKey, "done",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["ttlInSeconds"] = (7 * 24 * 3600).ToString() },
                    cancellationToken: cancellationToken);

                return Results.Accepted();
            });

        return app;
    }
}
