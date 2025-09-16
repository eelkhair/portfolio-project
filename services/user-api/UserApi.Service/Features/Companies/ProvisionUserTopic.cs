using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Events;

namespace UserApi.Features.Companies;

public class ProvisionUserTopic(ActivitySource activitySource, DaprClient client, IAuth0CommandService service, IMessageSender sender) : Endpoint<EventDto<ProvisionUserEvent>>
{
    public override void Configure()
    {
        Post("/Provision/user");
        AllowAnonymous();
         Options(c => 
             c.WithTopic(PubSubNames.RabbitMq, "provision.user"));
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
            await service.ProvisionUserAsync(request.Data, ct);
            activity2?.SetTag("user.email", request.Data.Email);
            activity2?.SetTag("company.uid", request.Data.CompanyUId);
            await sender.SendEventAsync(PubSubNames.RabbitMq, "provision.user.success", request.UserId, request.Data, ct);
            
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
