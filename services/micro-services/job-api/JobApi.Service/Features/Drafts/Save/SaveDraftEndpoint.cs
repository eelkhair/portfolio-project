using System.Diagnostics;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Drafts.Requests;
using JobAPI.Contracts.Models.Drafts.Responses;

namespace JobApi.Features.Drafts.Save;

public class SaveDraftEndpoint(IDraftCommandService service) : Endpoint<EventDto<SaveDraftRequest>, DraftResponse>
{
    public override void Configure()
    {
        Verbs(Http.PUT);
        Routes("/drafts/{companyUId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EventDto<SaveDraftRequest> request, CancellationToken ct)
    {
        var companyUId = Route<Guid>("companyUId");
        Activity.Current?.SetTag("draft.request.id", request.Data.Id);
        Activity.Current?.SetTag("draft.companyUid", companyUId);
        Activity.Current?.SetTag("userId", request.UserId);

        var response = await service.SaveDraftAsync(companyUId, request.Data, DaprExtensions.CreateUser(request.UserId), ct);

        Activity.Current?.SetTag("draft.saved.uid", response.Id);
        await Send.OkAsync(response, cancellation: ct);
    }
}
