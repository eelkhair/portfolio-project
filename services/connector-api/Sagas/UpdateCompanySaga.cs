using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Mappers;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyUpdated;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Sagas;

public class UpdateCompanySaga
{
    private readonly IMonolithClient _monolith;
    private readonly ICompanyApiClient _companyApi;
    private readonly IJobApiClient _jobApi;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<UpdateCompanySaga> _logger;

    public UpdateCompanySaga(
        IMonolithClient monolith,
        ICompanyApiClient companyApi,
        IJobApiClient jobApi,
        ILogger<UpdateCompanySaga> logger, ActivitySource activitySource)
    {
        _monolith = monolith;
        _companyApi = companyApi;
        _jobApi = jobApi;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task HandleAsync(
        EventDto<CompanyUpdatedV1Event> @event,
        CancellationToken ct)
    {
        using var sagaSpan = _activitySource.StartActivity("update.company.saga");
        sagaSpan?.SetTag("company.uid", @event.Data.CompanyUId);
        sagaSpan?.SetTag("userId", @event.UserId);

        try
        {
            _logger.LogInformation(
                "Saga started: UpdateCompanySaga Company {CompanyUId}",
                @event.Data.CompanyUId);

            CompanyUpdatedPayloads payloads;

            using (var fetchSpan = _activitySource.StartActivity("update.company.saga.fetch-data"))
            {
                var company = await _monolith.GetCompanyForUpdatedEventAsync(
                    @event.Data.CompanyUId,
                    @event.Data.UserId,
                    ct);

                payloads = CompanyUpdatedMapper.Map(@event.Data, company);
            }

            _logger.LogInformation("Saga step: Projections completed {CompanyUId}", @event.Data.CompanyUId);

            using (var fanOutSpan = _activitySource.StartActivity("update.company.saga.fan-out"))
            {
                await Task.WhenAll(
                    _companyApi.SendCompanyUpdatedAsync(@event.Data.CompanyUId, payloads.Company, ct),
                    _jobApi.SendCompanyUpdatedAsync(@event.Data.CompanyUId, payloads.Job, ct));
            }

            _logger.LogInformation(
                "Saga completed: UpdateCompanySaga Company {CompanyUId}",
                @event.Data.CompanyUId);
        }
        catch (Exception ex)
        {
            sagaSpan?.AddException(ex);
            sagaSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Saga failed: UpdateCompanySaga Company {CompanyUId}", @event.Data.CompanyUId);
            throw;
        }
    }
}
