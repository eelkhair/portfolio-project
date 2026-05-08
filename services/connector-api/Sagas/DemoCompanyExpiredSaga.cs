using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Sagas;

/// <summary>
/// Reverse of <see cref="CompanyProvisioningSaga"/> — torch a demo company in
/// dependency order: job-api jobs, company-api row, user-api Keycloak group + users,
/// and finally the monolith Company row. Idempotent — each step swallows 404s so
/// retries after a partial failure complete cleanly.
/// </summary>
public class DemoCompanyExpiredSaga(
    IMonolithClient monolith,
    ICompanyApiClient companyApi,
    IJobApiClient jobApi,
    IUserApiClient userApi,
    ActivitySource activitySource,
    ILogger<DemoCompanyExpiredSaga> logger)
{
    public async Task HandleAsync(EventDto<DemoCompanyExpiredV1Event> @event, CancellationToken ct)
    {
        var companyUId = @event.Data.CompanyUId;

        using var sagaSpan = activitySource.StartActivity("demo-company.expired.saga");
        sagaSpan?.SetTag("company.uid", companyUId);
        sagaSpan?.SetTag("userId", @event.UserId);

        logger.LogInformation("Saga started: DemoCompanyExpiredSaga {CompanyUId}", companyUId);

        await SafeStep("delete.jobs", () => jobApi.DeleteCompanyAsync(companyUId, ct));
        await SafeStep("delete.company-api", () => companyApi.DeleteCompanyAsync(companyUId, ct));
        await SafeStep("delete.user-api+keycloak", () => userApi.DeleteCompanyAsync(companyUId, ct));
        await SafeStep("delete.monolith", () => monolith.DeleteDemoCompanyAsync(companyUId, ct));

        logger.LogInformation("Saga completed: DemoCompanyExpiredSaga {CompanyUId}", companyUId);

        async Task SafeStep(string name, Func<Task> action)
        {
            using var stepSpan = activitySource.StartActivity($"demo-company.expired.{name}");
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                stepSpan?.AddException(ex);
                stepSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogWarning(ex,
                    "Demo cleanup step '{Step}' failed for {CompanyUId} — continuing (idempotent retry on next sweep)",
                    name, companyUId);
            }
        }
    }
}
