using System.Diagnostics;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Companies.Requests;

namespace JobApi.Features.Companies.Create;

public class CreateCompanyTopic(ICompanyCommandService service, ILogger<CreateCompanyTopic> logger) : Endpoint<EventDto<CreateCompanyRequest>>
{
    public override void Configure()
    {
        Post("companies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EventDto<CreateCompanyRequest> request, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "company");
        Activity.Current?.SetTag("entity.id", request.Data?.UId);
        Activity.Current?.SetTag("operation", "create");

        logger.LogInformation("Creating company - {CompanyName}", request.Data?.Name);
        await service.CreateCompanyAsync(new CreateCompanyRequest
        {
            UId = request.Data?.UId ?? Guid.Empty,
            Name = request.Data?.Name!,
        }, DaprExtensions.CreateUser(request.UserId), ct);
    }
}
