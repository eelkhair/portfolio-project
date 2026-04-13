using System.Diagnostics;
using AdminAPI.Contracts.Models.Settings;
using AdminAPI.Contracts.Services;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Settings.ApplicationMode;

public sealed class GetApplicationModeEndpoint(ISettingsCommandService settings)
    : EndpointWithoutRequest<ApiResponse<ApplicationModeDto>>
{
    private const string RouteTemplate = "settings/mode";

    public override void Configure()
    {
        Get(RouteTemplate);
        Summary(s =>
        {
            s.Summary = "Get current application mode";
            s.Description = "Returns the current application mode (e.g. 'monolith' or 'microservices').";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "settings");
        Activity.Current?.SetTag("operation", "get");
        var mode = await settings.GetApplicationModeAsync(ct);
        await Send.OkAsync(mode, ct);
    }

}
