using JobBoard.Monolith.Contracts.Companies;

namespace JobBoard.API.Infrastructure.SignalR.CompanyActivation;

/// <summary>
/// Represents a notifier for company-related events within the system.
/// </summary>
public interface ICompanyActivationNotifier
{
    /// <summary>
    /// Sends a notification about the creation of a new company.
    /// </summary>
    /// <param name="request">The details of the created company encapsulated in a <see cref="CompanyCreatedModel"/> instance.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyAsync(CompanyCreatedModel request, CancellationToken cancellationToken);
}