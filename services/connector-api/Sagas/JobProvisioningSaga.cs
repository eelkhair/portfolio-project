using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Mappers;
using ConnectorAPI.Models;
using Dapr.Client;
using JobBoard.IntegrationEvents.Job;

namespace ConnectorAPI.Sagas;

public class JobProvisioningSaga
{
    private readonly IJobApiClient _jobApi;
    private readonly DaprClient _daprClient;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<JobProvisioningSaga> _logger;

    public JobProvisioningSaga(
        IJobApiClient jobApi,
        DaprClient daprClient,
        ILogger<JobProvisioningSaga> logger,
        ActivitySource activitySource)
    {
        _jobApi = jobApi;
        _daprClient = daprClient;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task HandleAsync(
        EventDto<JobCreatedV1Event> @event,
        CancellationToken ct)
    {
        using var sagaSpan = _activitySource.StartActivity("provision.job.saga");
        sagaSpan?.SetTag("job.uid", @event.Data.UId);
        sagaSpan?.SetTag("job.companyUid", @event.Data.CompanyUId);
        sagaSpan?.SetTag("userId", @event.UserId);

        try
        {
            _logger.LogInformation(
                "Saga started: JobProvisioningSaga Job {JobUId}",
                @event.Data.UId);

            Models.JobCreated.JobApiResponse jobResponse;
            using (_activitySource.StartActivity("provision.job.saga.forward"))
            {
                var payload = JobCreatedMapper.Map(@event.Data);
                jobResponse = await _jobApi.SendJobCreatedAsync(payload, ct);
            }

            _logger.LogInformation("Saga step: Job forwarded to job-api {JobUId}", @event.Data.UId);
            
            using (_activitySource.StartActivity("provision.job.saga.publish"))
            {
                var publishedEvent = new EventDto<Models.JobCreated.JobApiResponse>(
                    @event.UserId,
                    Guid.CreateVersion7().ToString(),
                    jobResponse);
                publishedEvent.Data.UId = @event.Data.UId;
                publishedEvent.Data.DraftId = @event.Data.DraftId;

                await _daprClient.PublishEventAsync("rabbitmq.pubsub", "job.published.v2", publishedEvent, ct);
                _logger.LogInformation("Saga step: Published job.published event {JobUId}", @event.Data.UId);
            }

            _logger.LogInformation(
                "Saga completed: JobProvisioningSaga Job {JobUId}",
                @event.Data.UId);
        }
        catch (Exception ex)
        {
            sagaSpan?.AddException(ex);
            sagaSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Saga failed: JobProvisioningSaga Job {JobUId}", @event.Data.UId);
            throw;
        }
    }
}
