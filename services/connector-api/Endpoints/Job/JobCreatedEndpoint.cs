using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Company;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Job;

namespace ConnectorAPI.Endpoints.Job;

public static class JobCreatedEndpoint
{
    public static WebApplication MapJobCreatedEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/job",
            [Topic("rabbitmq.pubsub", "monolith.job-created.v1")]
            async (
                EventDto<JobCreatedV1Event> @event,
                JobProvisioningSaga saga,
                ActivitySource activitySource,
                DaprClient client,
                ILogger<JobCreatedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                using var parentSpan = activitySource.StartActivity("provision.job");
                parentSpan?.SetTag("job.uid", @event.Data.UId);
                parentSpan?.SetTag("job.companyUid", @event.Data.CompanyUId);
                parentSpan?.SetTag("event.type", @event.EventType);
                parentSpan?.SetTag("idempotency.key", @event.IdempotencyKey);
                parentSpan?.SetTag("userId", @event.UserId);

                var stateKey = $"{IdempotencyOptions.Prefix}{@event.IdempotencyKey}";
                using (var spanIdempotency =
                       activitySource.StartActivity("provision.job.idempotency"))
                {
                    spanIdempotency?.SetTag("idempotency.state_key", stateKey);

                    logger.LogInformation("Received job created event {JobUId}", @event.Data.UId);
                    logger.LogDebug("Checking idempotency key {IdempotencyKey}", @event.IdempotencyKey);
                    var existing = await client.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: cancellationToken);

                    if (existing is not null)
                    {
                        spanIdempotency?.SetTag("idempotency.duplicate", true);
                        logger.LogInformation("Skipping job provisioning. Idempotency key already processed");
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
                    logger.LogError(ex, "Unhandled error while provisioning job {JobUId}", @event.Data.UId);
                    return Results.Accepted();
                }

                using (activitySource.StartActivity("provision.job.finalize"))
                {
                    logger.LogInformation("Marking job provisioning workflow as completed");

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

                logger.LogInformation("Job {JobUId} provisioned successfully", @event.Data.UId);
                return Results.Accepted();
            });

        return app;
    }
}
