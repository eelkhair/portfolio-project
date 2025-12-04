using System.Diagnostics;
using ConnectorAPI.Helpers;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Endpoints;

public record UserDto(string FirstName, string LastName, string Email);
public record CompanyDto(string Name, string Description, string Website);
public static class CompanyEndpoints
{

    public static WebApplication SetupCompanyEndpoints(this WebApplication app)
    {
        app.MapPost("/connector/company",
            [Topic("rabbitmq.pubsub", "outbox-events")]
            async (EventDto<CompanyCreatedV1Event> companyEvent, DaprClient client, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;
                var logger = loggerFactory.CreateLogger("CompanyCreatedV1Event");
    
                logger.LogInformation("Received event {TraceId}", traceId);
                
                var companyTask =  client.InvokeMethodAsync<CompanyDto>("monolith-api", $"odata/companies/{companyEvent.Data.CompanyUId}", cancellationToken: cancellationToken);
                var adminTask = client.InvokeMethodAsync<UserDto>("monolith-api", $"odata/users/{companyEvent.Data.AdminUId}", cancellationToken: cancellationToken);

                await Task.WhenAll(companyTask, adminTask);
                
                var company = await companyTask;
                var admin = await adminTask;
                
                return Results.Ok();
            });
        return app;
    }
}