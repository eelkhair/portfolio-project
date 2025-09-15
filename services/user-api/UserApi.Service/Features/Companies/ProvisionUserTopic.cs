using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Events;
using UserAPI.Contracts.Models.Requests;

namespace UserApi.Features.Companies;

public class ProvisionUserTopic(ActivitySource activitySource, DaprClient client, IUserCommandService service) : Endpoint<EventDto<ProvisionUserEvent>>
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
        activity?.Stop();
        
        
       await service.ProvisionUserAsync(request.Data, ct);

        await client.SaveStateAsync(
            storeName: StateStores.Redis,
            key: $"Processed:{request.IdempotencyKey}",
            value: "done",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = (7*24*3600).ToString() },
            cancellationToken: ct);

        await SendOkAsync(ct);
    }
}
