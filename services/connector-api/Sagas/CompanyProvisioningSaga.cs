using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Mappers;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Sagas;

public class CompanyProvisioningSaga
{
    private readonly IMonolithClient _monolith;
    private readonly ICompanyApiClient _companyApi;
    private readonly IJobApiClient _jobApi;
    private readonly IUserApiClient _userApi;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<CompanyProvisioningSaga> _logger;

    public CompanyProvisioningSaga(
        IMonolithClient monolith,
        ICompanyApiClient companyApi,
        IJobApiClient jobApi,
        IUserApiClient userApi,
        ILogger<CompanyProvisioningSaga> logger, ActivitySource activitySource)
    {
        _monolith = monolith;
        _companyApi = companyApi;
        _jobApi = jobApi;
        _userApi = userApi;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task HandleAsync(
        EventDto<CompanyCreatedV1Event> @event,
        CancellationToken ct)
    {
        using var sagaSpan = _activitySource.StartActivity("provision.company.saga");
        sagaSpan?.SetTag("company.uid", @event.Data.CompanyUId);
        sagaSpan?.SetTag("company.admin.uid", @event.Data.AdminUId);
        sagaSpan?.SetTag("userId", @event.UserId);

        try
        {
            _logger.LogInformation(
                "Saga started: CompanyProvisioningSaga Company {CompanyUId}",
                @event.Data.CompanyUId);

            CompanyCreateCompanyResult company;
            CompanyCreatedPayloads payloads;

            using (var fetchSpan = _activitySource.StartActivity("provision.company.saga.fetch-data"))
            {
                var (fetchedCompany, admin) =
                    await _monolith.GetCompanyAndAdminForCreatedEventAsync(
                        @event.Data.CompanyUId,
                        @event.Data.AdminUId,
                        @event.Data.UserId,
                        ct);

                company = fetchedCompany;
                payloads = CompanyCreatedMapper.Map(@event.Data, company, admin);
            }

            _logger.LogInformation("Saga step: Projections completed {CompanyUId}", @event.Data.CompanyUId);

            CompanyCreatedUserApiPayload userResult;
            using (var fanOutSpan = _activitySource.StartActivity("provision.company.saga.fan-out"))
            {
                var userTask = _userApi.SendCompanyCreatedAsync(payloads.User, ct);

                await Task.WhenAll(
                    _companyApi.SendCompanyCreatedAsync(payloads.Company, ct),
                    _jobApi.SendCompanyCreatedAsync(payloads.Job, ct),
                    userTask);

                userResult = await userTask;
            }

            _logger.LogInformation("Saga step: User provisioned {CompanyUId}", @event.Data.CompanyUId);

            using (var activateSpan = _activitySource.StartActivity("provision.company.saga.activate"))
            {
                await _monolith.ActivateCompanyAsync(
                    @event.Data,
                    company,
                    userResult,
                    ct);

                _logger.LogInformation("Saga step: Activation completed {CompanyUId}", @event.Data.CompanyUId);
            }

            _logger.LogInformation(
                "Saga completed: CompanyProvisioningSaga Company {CompanyUId}",
                @event.Data.CompanyUId);
        }
        catch (Exception ex)
        {
            sagaSpan?.AddException(ex);
            sagaSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Saga failed: CompanyProvisioningSaga Company {CompanyUId}", @event.Data.CompanyUId);
            throw;
        }
    }
}
