using System.Diagnostics;
using AdminApi.Infrastructure;
using CompanyAPI.Contracts.Models.Companies;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UserAPI.Contracts.Models.Events;

namespace AdminApi.Features.Companies.Events;

public class CompanyActivatedSubscriber(
    ILogger<CompanyActivatedSubscriber> log,
    IHubContext<NotificationsHub> hub, ActivitySource activitySource) 
    : Endpoint<EventDto<CompanyCreatedSuccess>, OkObjectResult>
{
    public override void Configure()
    {
        Post("/events/company-activated");
        AllowAnonymous();
        Options(o => o.WithTopic(PubSubNames.RabbitMq, "company.create.success"));
    }

    public override async Task HandleAsync(EventDto<CompanyCreatedSuccess> e, CancellationToken ct)
    {
        try
        {
            using var act = activitySource.StartActivity(
                "signalr.message.send",
                ActivityKind.Producer);

            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", "CompanyActivated");
            act?.SetTag("messaging.operation", "send");
            var parent = Activity.Current; 
            await hub.Clients.Group(e.UserId).SendAsync("CompanyActivated", new
            {
                e.Data.CompanyUId,
                e.Data.CompanyName,
                TraceParent =parent?.Id,                 
                TraceState =parent?.TraceStateString,    
                Message = $"“{e.Data.CompanyName}” has been activated."
            }, ct);
            act?.SetTag("enduser.id", e.UserId);
            act?.SetTag("company.id", e.Data.CompanyUId);
            act?.SetTag("company.name", e.Data.CompanyName);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push CompanyActivated to user {UserId} for {CompanyUId}", e.UserId, e.Data.CompanyUId);
        }

        await Send.OkAsync(ct);
    }
}