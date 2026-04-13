using System.Diagnostics;
using AdminAPI.Contracts.Services;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Industries.List;


public class ListIndustriesEndpoint(IIndustryQueryService service) : EndpointWithoutRequest<ApiResponse<List<IndustryResponse>>>
{
    public override void Configure()
    {
        Get("/industries");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "industry");
        Activity.Current?.SetTag("operation", "list");
        var industries = await service.ListAsync(ct);
        await Send.OkAsync(industries, cancellation: ct);
    }
}
