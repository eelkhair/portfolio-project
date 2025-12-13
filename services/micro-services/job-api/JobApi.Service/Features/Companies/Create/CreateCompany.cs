using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using JobApi.Application.Interfaces;
using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Requests;

namespace JobApi.Features.Companies.Create;

public class CreateCompanyTopic(ICompanyCommandService service, ILogger<CreateCompanyTopic> logger):  Endpoint<EventDto<CreateCompanyRequest>>
{
    public override void Configure()
    {
        Post("companies");
    }
    
    public override async  Task HandleAsync(EventDto<CreateCompanyRequest> request, CancellationToken ct)
    {
        logger.LogInformation("Creating company - {CompanyName}", request.Data?.Name);
        await service.CreateCompanyAsync(new CreateCompanyRequest
        {
            UId = request.Data?.UId ?? Guid.Empty,
            Name = request.Data?.Name!, 
        }, DaprExtensions.CreateUser(request.UserId), ct);
    }
}