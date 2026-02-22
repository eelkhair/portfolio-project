using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Settings;
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
        var mode = await settings.GetApplicationModeAsync(ct);
        await Send.OkAsync(mode, ct);
    }
    
}