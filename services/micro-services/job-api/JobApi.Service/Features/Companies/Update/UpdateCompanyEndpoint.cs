using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Companies.Requests;

namespace JobApi.Features.Companies.Update;

public class UpdateCompanyEndpoint(ICompanyCommandService service, ILogger<UpdateCompanyEndpoint> logger)
    : Endpoint<EventDto<UpdateCompanyRequest>>
{
    public override void Configure()
    {
        Put("companies/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EventDto<UpdateCompanyRequest> request, CancellationToken ct)
    {
        var companyUId = Route<Guid>("id");
        logger.LogInformation("Updating Company: {CompanyUId}", companyUId);
        await service.UpdateCompanyAsync(companyUId, request.Data, DaprExtensions.CreateUser(request.UserId), ct);
    }
}
