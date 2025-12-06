using System.Diagnostics;
using System.Web;
using Azure.Identity;
using ConnectorAPI.Helpers;
using ConnectorAPI.Models;
using Dapr;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Endpoints;

public static class CompanyEndpoints
{

    public static WebApplication SetupCompanyEndpoints(this WebApplication app)
{
    app.MapPost("/connector/company",
        [Topic("rabbitmq.pubsub", "outbox-events")]
        async (EventDto<CompanyCreatedV1Event> companyEvent,
               DaprClient client,
               ILoggerFactory loggerFactory,
               CancellationToken cancellationToken) =>
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;
            var logger = loggerFactory.CreateLogger("CompanyCreatedV1Event");

            logger.LogInformation("Received event {TraceId}", traceId);
            
            var companyRoute = BuildODataRoute(
                $"odata/companies/{companyEvent.Data.CompanyUId}/",
                new Dictionary<string, string>
                {
                    ["$select"] = "name, website",
                    ["$expand"] = "industry($select=uid,name)"
                }
            );

            var userRoute = BuildODataRoute(
                $"odata/users/{companyEvent.Data.AdminUId}/",
                new Dictionary<string, string>
                {
                    ["$select"] = "firstname,lastname,email,uid"
                }
            );
            
            var headers = new Dictionary<string, string>
            {
                ["x-user-id"] = companyEvent.Data.UserId
            };
            HttpRequestMessage BuildRequest(string route)
            {
                var req = client.CreateInvokeMethodRequest(HttpMethod.Get, "monolith-api", route);

                foreach (var kvp in headers)
                    req.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);

                return req;
            }

            var companyRequest = BuildRequest(companyRoute);
            var adminRequest = BuildRequest(userRoute);

            var companyTask = client.InvokeMethodAsync<CompanyDto>(companyRequest, cancellationToken);

           // var adminTask = client.InvokeMethodAsync<UserDto>(adminRequest, cancellationToken);

            var company = await companyTask;
         //   var admin = await adminTask;
Console.WriteLine(company.Name);
            // TODO: Build payload, forward to Admin API, etc.

            return Results.Ok();
        });

    return app;
}
    
    private static string BuildODataRoute(string path, IDictionary<string, string> query)
    {
        var qs = HttpUtility.ParseQueryString(string.Empty);

        foreach (var kvp in query)
            qs[kvp.Key] = kvp.Value;

        return $"{path}?{qs}";
    }
}