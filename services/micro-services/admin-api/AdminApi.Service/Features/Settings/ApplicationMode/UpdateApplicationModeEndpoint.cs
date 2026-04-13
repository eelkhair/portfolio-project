using System.Diagnostics;
using AdminAPI.Contracts.Models.Settings;
using AdminAPI.Contracts.Services;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Settings.ApplicationMode;

public class UpdateApplicationModeEndpoint(ISettingsCommandService settings)
    : Endpoint<ApplicationModeDto, ApiResponse<ApplicationModeDto>>
{
    private const string RouteTemplate = "settings/mode";

    public override void Configure()
    {
        Put(RouteTemplate);
        Summary(s =>
        {
            s.Summary = "Update application mode";
            s.Description = "Switches between monolith and microservices mode.";
        });
    }

    public override async Task HandleAsync(ApplicationModeDto req, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "settings");
        Activity.Current?.SetTag("operation", "set");
        var result = await settings.UpdateApplicationModeAsync(req, ct);
        await Send.OkAsync(result, ct);
    }
}
