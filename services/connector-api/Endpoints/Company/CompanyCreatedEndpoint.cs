using System.Diagnostics;
using ConnectorAPI.Mappers;
using ConnectorAPI.Models;
using ConnectorAPI.Services;
using Dapr;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Endpoints.Company;

public static class CompanyCreatedEndpoint
{
    public static WebApplication MapCompanyCreatedEndpoint(this WebApplication app)
    {
        app.MapPost("/connector/company",
            [Topic("rabbitmq.pubsub", "outbox-events")]
            async (
                EventDto<CompanyCreatedV1Event> @event,
                IMonolithClient monolithODataClient,
                IAdminApiClient adminApiClient,
                ILogger<CompanyCreatedV1Event> logger,
                CancellationToken cancellationToken) =>
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;

                logger.LogInformation("Received CompanyCreatedV1Event {TraceId} for Company {CompanyId}, Admin {AdminId}, UserId {@UserId}, UserCompanyId {@UserCompanyId}",
                    traceId,
                    @event.Data.CompanyUId,
                    @event.Data.AdminUId,
                    @event.Data.UserId,
                    @event.Data.UserCompanyUId);

                var (company, admin) = await monolithODataClient.GetCompanyAndAdminForCreatedEventAsync(
                    @event.Data.CompanyUId,
                    @event.Data.AdminUId,
                    @event.Data.UserId,
                    cancellationToken);

                var payload = CompanyCreatedMapper.Map(@event.Data, company, admin);

                await adminApiClient.SendCompanyCreatedAsync(payload, cancellationToken);

                logger.LogInformation("Successfully processed CompanyCreatedV1Event {TraceId}", traceId);

                return Results.Accepted();
            });

        return app;
    }
}