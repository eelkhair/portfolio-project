using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Auth0.ManagementApi.Models;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Events;
using UserAPI.Contracts.Models.Requests;

namespace UserApi.Features.Companies;

public class ProvisionUserTopic(ActivitySource activitySource, DaprClient client, IAuth0CommandService aut0Service, IMessageSender sender, ICompanyCommandService commandService) : Endpoint<EventDto<ProvisionUserEvent>>
{
    public override void Configure()
    {
        Post("events/company/create");
        AllowAnonymous();
         Options(c => 
             c.WithTopic(PubSubNames.RabbitMq, "company.created"));
    }

    public override async Task HandleAsync(EventDto<ProvisionUserEvent> request, CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("Checking Idempotency.");
        var existing = await client.GetStateAsync<string>(StateStores.Redis,$"Processed:{request.IdempotencyKey}", cancellationToken:ct);
        if (existing is not null) { await SendOkAsync(ct); return; }
        await client.SaveStateAsync(StateStores.Redis, $"Processed:{request.IdempotencyKey}", "Processing",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = "120" },
            cancellationToken: ct);
        activity?.SetTag("user.email", request.Data.Email);
        activity?.SetTag("company.uid", request.Data.CompanyUId);
        activity?.SetTag("user.first-name", request.Data.FirstName);
        
        
        try
        {
            using var activity2 = activitySource.StartActivity("Provisioning User.");
            var (auth0User, auth0Company)= await aut0Service.ProvisionUserAsync(request.Data, ct);
        
            if (auth0User is not null &&  auth0Company is not null)
            {
                using var activity4 = activitySource.StartActivity("Saving to database.");
                var principal = DaprExtensions.CreateUser(request.UserId);
                var userId = await commandService.CreateUser(new CreateUserRequest
                {
                    Auth0Id = auth0User.UserId,
                    FirstName = auth0User.FirstName,
                    LastName = auth0User.LastName,
                    Email = auth0User.Email
                }, principal, ct);
                var companyId = await commandService.CreateCompany(new CreateCompanyRequest()
                {
                    Auth0OrganizationId = auth0Company.Id,
                    Name = auth0Company.DisplayName,
                    UId = request.Data.CompanyUId,
                }, principal, ct);
                await commandService.AddUserToCompany(userId, companyId, principal, ct);
                
                activity4?.SetTag("user.id", userId);
                activity4?.SetTag("company.id", companyId);
                activity4?.SetTag("auth0.user.id", auth0User.UserId);
                activity4?.SetTag("auth0.company.id", auth0Company.Id);
                activity2?.SetTag("user.email", request.Data.Email);
                activity2?.SetTag("company.uid", request.Data.CompanyUId);
                await sender.SendEventAsync(PubSubNames.RabbitMq, "provision.user.success", request.UserId, request.Data, ct);

            }
        }
        catch (ArgumentException e)
        {
            activity?.SetTag("error", e.Message);
            activity?.SetTag("user.email", request.Data.Email);
            activity?.SetTag("company.uid", request.Data.CompanyUId);
            await sender.SendEventAsync(PubSubNames.RabbitMq, "provision.user.error", request.UserId, e.Message, ct);
        }
       
        using var activity3 = activitySource.StartActivity("Marking as Processed.");
        await client.SaveStateAsync(
            storeName: StateStores.Redis,
            key: $"Processed:{request.IdempotencyKey}",
            value: "done",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = (7*24*3600).ToString() },
            cancellationToken: ct);

        activity3?.SetTag("user.email", request.Data.Email);
        activity3?.SetTag("company.uid", request.Data.CompanyUId);

       
        
        
        
        await SendOkAsync(ct);
    }
}
