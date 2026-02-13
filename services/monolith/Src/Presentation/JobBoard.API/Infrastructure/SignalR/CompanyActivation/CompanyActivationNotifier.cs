using System.Diagnostics;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.AspNetCore.SignalR;

namespace JobBoard.API.Infrastructure.SignalR.CompanyActivation;

/// <summary>
/// Responsible for handling company activation notifications and sending them
/// to the appropriate users through SignalR.
/// </summary>
/// <remarks>
/// This class utilizes the SignalR <see cref="IHubContext{T}"/> to deliver real-time
/// notifications. It also integrates with OpenTelemetry <see cref="ActivitySource"/>
/// for tracing distributed operations and logs any failures using a logging provider.
/// </remarks>
public class CompanyActivationNotifier(
    IHubContext<NotificationsHub> hub,
    ActivitySource activitySource,
    ILogger<CompanyActivationNotifier> log) : ICompanyActivationNotifier
{
    /// <summary>
    /// Sends a notification about company activation to the user who created the company,
    /// utilizing SignalR for real-time communication.
    /// </summary>
    /// <param name="request">An instance of <see cref="CompanyCreatedModel"/> containing the details of the created company
    /// and the user who initiated the operation.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation of sending the notification.</returns>
    public async Task NotifyAsync(CompanyCreatedModel request, CancellationToken cancellationToken)
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
            await hub.Clients.Group(request.CreatedBy).SendAsync("CompanyActivated", new
            {
                request.CompanyUId,
                request.CompanyName,
                TraceParent =parent?.Id,                 
                TraceState =parent?.TraceStateString,    
                Message = $"“{request.CompanyName}” has been activated."
            }, cancellationToken);
            act?.SetTag("enduser.id", request.CreatedBy);
            act?.SetTag("company.id", request.CompanyUId);
            act?.SetTag("company.name", request.CompanyName);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push CompanyActivated to user {UserId} for {CompanyUId}", request.CreatedBy, request.CompanyUId);
        }
    }
}