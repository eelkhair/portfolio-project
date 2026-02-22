using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Settings;
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
        var result = await settings.UpdateApplicationModeAsync(req, ct);
        await Send.OkAsync(result, ct);
    }
}