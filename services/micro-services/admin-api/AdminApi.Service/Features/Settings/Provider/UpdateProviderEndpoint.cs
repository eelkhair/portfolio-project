using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Settings;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Settings.Provider;

public sealed class UpdateProviderEndpoint(ISettingsCommandService settings)
    : Endpoint<UpdateProviderRequest, ApiResponse<UpdateProviderResponse>>
{
    private const string RouteTemplate = "settings/update-provider";

    public override void Configure()
    {
        Put(RouteTemplate);
        Summary(s =>
        {
            s.Summary = "Update AI provider settings";
            s.Description = "Updates the AI provider and model used for job generation.";
        });
    }

    public override async Task HandleAsync(UpdateProviderRequest req, CancellationToken ct)
    {
        var result = await settings.UpdateProviderAsync(req, ct);
        await Send.OkAsync(result, ct);
    }
}
