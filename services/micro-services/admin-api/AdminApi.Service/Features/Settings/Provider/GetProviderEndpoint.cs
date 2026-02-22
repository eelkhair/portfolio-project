using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Settings;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Settings.Provider;

public sealed class GetProviderEndpoint(ISettingsCommandService settings)
    : EndpointWithoutRequest<ApiResponse<GetProviderResponse>>
{
    private const string RouteTemplate = "settings/provider";

    public override void Configure()
    {
        Get(RouteTemplate);
        Summary(s =>
        {
            s.Summary = "Get current AI provider settings";
            s.Description = "Returns the current AI provider and model configuration.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await settings.GetProviderAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
