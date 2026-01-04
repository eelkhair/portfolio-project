using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Companies.Requests;
using UserAPI.Contracts.Models.Events;

namespace JobApi.Features.Companies.Events;

public class CreateCompanyTopic(ICompanyCommandService service, ILogger<CreateCompanyTopic> logger):  Endpoint<EventDto<ProvisionUserEvent>>
{
    public override void Configure()
    {
        Post("events/company/create");
        AllowAnonymous();
        Options(c => 
            c.WithTopic(PubSubNames.RabbitMq, "company.created"));
    }
    
    public override async  Task HandleAsync(EventDto<ProvisionUserEvent> request, CancellationToken ct)
    {
        logger.LogInformation("Creating company - {CompanyName}", request.Data?.CompanyName);
        await service.CreateCompanyAsync(new CreateCompanyRequest
        {
            UId = request.Data?.CompanyUId ?? Guid.Empty,
            Name = request.Data?.CompanyName!, 
        }, DaprExtensions.CreateUser(request.UserId), ct);
    }
}