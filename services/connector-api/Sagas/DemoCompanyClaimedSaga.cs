using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.DemoCompany;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Sagas;

/// <summary>
/// Repoint a demo company's admin user reference from the synthetic
/// demo-{guid}@demo.elkhair.tech account to the real user that just signed up
/// via Keycloak. Each microservice clears its local IsDemo flag and updates its
/// admin projection so the company becomes a normal authenticated company.
/// </summary>
public class DemoCompanyClaimedSaga(
    ICompanyApiClient companyApi,
    IJobApiClient jobApi,
    IUserApiClient userApi,
    ActivitySource activitySource,
    ILogger<DemoCompanyClaimedSaga> logger)
{
    public async Task HandleAsync(EventDto<DemoCompanyClaimedV1Event> @event, CancellationToken ct)
    {
        var data = @event.Data;
        var payload = new DemoCompanyClaimedPayload
        {
            NewAdminUserUId = data.NewAdminUserUId,
            NewAdminEmail = data.NewAdminEmail,
            NewAdminFirstName = data.NewAdminFirstName,
            NewAdminLastName = data.NewAdminLastName,
            SyntheticAdminUserUId = data.SyntheticAdminUserUId
        };

        using var sagaSpan = activitySource.StartActivity("demo-company.claimed.saga");
        sagaSpan?.SetTag("company.uid", data.CompanyUId);
        sagaSpan?.SetTag("new.admin.uid", data.NewAdminUserUId);
        sagaSpan?.SetTag("synthetic.admin.uid", data.SyntheticAdminUserUId);
        sagaSpan?.SetTag("userId", @event.UserId);

        logger.LogInformation(
            "Saga started: DemoCompanyClaimedSaga {CompanyUId} → {NewAdminEmail}",
            data.CompanyUId, data.NewAdminEmail);

        // user-api MUST run first — it owns Keycloak group membership; downstream services
        // expect the new admin to already exist in Keycloak before they repoint their
        // foreign keys onto it.
        await Step("claim.user-api+keycloak", () => userApi.ClaimCompanyAsync(data.CompanyUId, payload, ct));

        // Company-api + job-api can fan-out in parallel — they only update their local
        // admin reference + IsDemo flag.
        await Task.WhenAll(
            Step("claim.company-api", () => companyApi.ClaimCompanyAsync(data.CompanyUId, payload, ct)),
            Step("claim.job-api", () => jobApi.ClaimCompanyAsync(data.CompanyUId, payload, ct)));

        logger.LogInformation("Saga completed: DemoCompanyClaimedSaga {CompanyUId}", data.CompanyUId);

        async Task Step(string name, Func<Task> action)
        {
            using var stepSpan = activitySource.StartActivity($"demo-company.claimed.{name}");
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                stepSpan?.AddException(ex);
                stepSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(ex,
                    "Claim saga step '{Step}' failed for {CompanyUId} — partial state. Manual cleanup may be required.",
                    name, data.CompanyUId);
                throw;
            }
        }
    }
}
