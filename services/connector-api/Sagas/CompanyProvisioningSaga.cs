using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Mappers;
using ConnectorAPI.Models;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Sagas;

public class CompanyProvisioningSaga
{
    private readonly IMonolithClient _monolith;
    private readonly ICompanyApiClient _companyApi;
    private readonly IJobApiClient _jobApi;
    private readonly IUserApiClient _userApi;
    private readonly ILogger<CompanyProvisioningSaga> _logger;

    public CompanyProvisioningSaga(
        IMonolithClient monolith,
        ICompanyApiClient companyApi,
        IJobApiClient jobApi,
        IUserApiClient userApi,
        ILogger<CompanyProvisioningSaga> logger)
    {
        _monolith = monolith;
        _companyApi = companyApi;
        _jobApi = jobApi;
        _userApi = userApi;
        _logger = logger;
    }

    public async Task HandleAsync(
        EventDto<CompanyCreatedV1Event> @event,
        CancellationToken ct)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;

        _logger.LogInformation(
            "Saga started: CompanyProvisioningSaga {TraceId} Company {CompanyUId}",
            traceId,
            @event.Data.CompanyUId);

        var (company, admin) =
            await _monolith.GetCompanyAndAdminForCreatedEventAsync(
                @event.Data.CompanyUId,
                @event.Data.AdminUId,
                @event.Data.UserId,
                ct);
        
        var payloads =
            CompanyCreatedMapper.Map(@event.Data, company, admin);
        
        _logger.LogInformation("Saga step: Projections completed {CompanyUId}", @event.Data.CompanyUId);

        var userTask =
            _userApi.SendCompanyCreatedAsync(payloads.User, ct);

        await Task.WhenAll(
            _companyApi.SendCompanyCreatedAsync(payloads.Company, ct),
            _jobApi.SendCompanyCreatedAsync(payloads.Job, ct),
            userTask);

        var userResult = await userTask;
        _logger.LogInformation("Saga step: User provisioned {CompanyUId}", @event.Data.CompanyUId);

        await _monolith.ActivateCompanyAsync(
            @event.Data,
            company,
            userResult,
            ct);

        _logger.LogInformation("Saga step: Activation completed {CompanyUId}", @event.Data.CompanyUId);

        _logger.LogInformation(
            "Saga completed: CompanyProvisioningSaga {TraceId} Company {CompanyUId}",
            traceId,
            @event.Data.CompanyUId);
    }
}
