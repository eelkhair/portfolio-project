using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Mappers;
using ConnectorAPI.Models;
using JobBoard.IntegrationEvents.Draft;

namespace ConnectorAPI.Sagas;

public class DraftSyncSaga
{
    private readonly IJobApiClient _jobApi;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<DraftSyncSaga> _logger;

    public DraftSyncSaga(
        IJobApiClient jobApi,
        ILogger<DraftSyncSaga> logger,
        ActivitySource activitySource)
    {
        _jobApi = jobApi;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task HandleSaveAsync(
        EventDto<DraftSavedV1Event> @event,
        CancellationToken ct)
    {
        using var sagaSpan = _activitySource.StartActivity("sync.draft.save.saga");
        sagaSpan?.SetTag("draft.uid", @event.Data.UId);
        sagaSpan?.SetTag("draft.companyUid", @event.Data.CompanyUId);
        sagaSpan?.SetTag("userId", @event.UserId);

        try
        {
            _logger.LogInformation(
                "Saga started: DraftSyncSaga Save Draft {DraftUId} for Company {CompanyUId}",
                @event.Data.UId, @event.Data.CompanyUId);

            var payload = new EventDto<Models.Drafts.SaveDraftPayload>(
                @event.UserId,
                Guid.CreateVersion7().ToString(),
                DraftSavedMapper.Map(@event.Data));
            sagaSpan?.SetTag("draft.payload.id", payload.Data.Id);

            await _jobApi.SaveDraftAsync(@event.Data.CompanyUId, payload, ct);

            _logger.LogInformation(
                "Saga completed: DraftSyncSaga Save Draft {DraftUId}",
                @event.Data.UId);
        }
        catch (Exception ex)
        {
            sagaSpan?.AddException(ex);
            sagaSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Saga failed: DraftSyncSaga Save Draft {DraftUId}", @event.Data.UId);
            throw;
        }
    }

    public async Task HandleDeleteAsync(
        EventDto<DraftDeletedV1Event> @event,
        CancellationToken ct)
    {
        using var sagaSpan = _activitySource.StartActivity("sync.draft.delete.saga");
        sagaSpan?.SetTag("draft.uid", @event.Data.UId);
        sagaSpan?.SetTag("draft.companyUid", @event.Data.CompanyUId);
        sagaSpan?.SetTag("userId", @event.UserId);

        try
        {
            _logger.LogInformation(
                "Saga started: DraftSyncSaga Delete Draft {DraftUId}",
                @event.Data.UId);

            await _jobApi.DeleteDraftAsync(@event.Data.UId, @event.UserId, ct);

            _logger.LogInformation(
                "Saga completed: DraftSyncSaga Delete Draft {DraftUId}",
                @event.Data.UId);
        }
        catch (Exception ex)
        {
            sagaSpan?.AddException(ex);
            sagaSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Saga failed: DraftSyncSaga Delete Draft {DraftUId}", @event.Data.UId);
            throw;
        }
    }
}
