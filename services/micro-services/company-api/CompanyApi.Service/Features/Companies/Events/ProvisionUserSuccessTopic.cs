using System.Diagnostics;
using CompanyApi.Application.Commands.Interfaces;
using CompanyAPI.Contracts.Models.Companies;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using UserAPI.Contracts.Models.Events;

namespace CompanyApi.Features.Companies.Events;


public class ProvisionUserSuccessTopic(ICompanyCommandService service, IMessageSender sender, ActivitySource activitySource): Endpoint<EventDto<ProvisionUserEvent>>
{
    public override void Configure()
    {
        Post("events/provision-user-success");
        AllowAnonymous();
        Options(c => 
            c.WithTopic(PubSubNames.RabbitMq, "provision.user.success"));
    }
    
    public override async Task HandleAsync(EventDto<ProvisionUserEvent> request, CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("Activating Company");
        activity?.SetTag("CompanyUId", request.Data.CompanyUId.ToString());
        activity?.SetTag("Name", request.Data?.CompanyName);

        var activated = await service.ActivateAsync(request.Data?.CompanyUId ?? Guid.Empty,
            DaprExtensions.CreateUser(request.UserId), ct);
        if (request.Data is not null && request.Data.SourceSystem == "AdminApi")
        {
            if (activated)
            {
                await sender.SendEventAsync(PubSubNames.RabbitMq, "company.create.success", request.UserId,
                    new CompanyCreatedSuccess
                    {
                        CompanyEmail = request.Data?.Email ?? string.Empty,
                        CompanyName = request.Data?.CompanyName ?? string.Empty,
                        CompanyUId = request.Data?.CompanyUId ?? Guid.Empty,
                        UserUId = request.Data?.UId ?? Guid.Empty,
                        Auth0CompanyId = request.Data?.Auth0OrganizationId ?? string.Empty,
                        Auth0UserId = request.Data?.Auth0UserId ?? string.Empty
                    }, ct);
            }
            else
            {
                await sender.SendEventAsync(PubSubNames.RabbitMq, "company.create.fail", request.UserId, request.Data,
                    ct);
            }
        }
    }
}