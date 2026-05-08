using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Endpoints.Company;

public static class DemoCompanyExpiredEndpoint
{
    public static WebApplication MapDemoCompanyExpiredEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/demo-company/expired",
            [Topic("rabbitmq.pubsub", "monolith.demo-company-expired.v1")]
            async (
                EventDto<DemoCompanyExpiredV1Event> @event,
                DemoCompanyExpiredSaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<DemoCompanyExpiredV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("demo-company.expired");
                parentSpan?.SetTag("sync.direction", "forward");
                parentSpan?.SetTag("sync.entity", "demo-company");
                parentSpan?.SetTag("company.uid", @event.Data.CompanyUId);
                parentSpan?.SetTag("event.type", @event.EventType);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);

                var stateKey = $"DemoExpired:{@event.IdempotencyKey}";
                var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);
                if (existing is not null)
                {
                    logger.LogInformation("Skipping demo expiry — idempotency key already processed: {Key}", stateKey);
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
                    logger.LogError(ex, "Unhandled error during demo company expiry {CompanyUId}", @event.Data.CompanyUId);
                    return Results.Accepted();
                }

                await client.SaveStateAsync(StateStores.Redis, stateKey, "done",
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["ttlInSeconds"] = (7 * 24 * 3600).ToString() },
                    cancellationToken: cancellationToken);

                return Results.Accepted();
            });

        return app;
    }
}
