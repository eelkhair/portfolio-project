using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Applications.Admin;

public class UpdateApplicationStatusCommand(Guid applicationId, string status)
    : BaseCommand<AdminApplicationListItem>
{
    public Guid ApplicationId { get; } = applicationId;
    public string Status { get; } = status;
}

public class UpdateApplicationStatusCommandHandler(
    IJobBoardDbContext context,
    ILogger<UpdateApplicationStatusCommandHandler> logger)
    : IHandler<UpdateApplicationStatusCommand, AdminApplicationListItem>
{
    public async Task<AdminApplicationListItem> HandleAsync(
        UpdateApplicationStatusCommand command, CancellationToken cancellationToken)
    {
        var newStatus = Enum.Parse<ApplicationStatus>(command.Status, true);

        var app = await context.JobApplications
            .Include(a => a.Job).ThenInclude(j => j.Company)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == command.ApplicationId, cancellationToken)
            ?? throw new NotFoundException($"Application {command.ApplicationId} not found");

        app.SetStatus(newStatus);
        await context.SaveChangesAsync(command.UserId, cancellationToken);

        logger.LogInformation("Application {ApplicationId} status updated to {Status}",
            command.ApplicationId, newStatus);

        return new AdminApplicationListItem
        {
            Id = app.Id,
            ApplicantName = $"{app.User.FirstName} {app.User.LastName}".Trim(),
            ApplicantEmail = app.User.Email,
            JobId = app.Job.Id,
            JobTitle = app.Job.Title,
            CompanyName = app.Job.Company.Name,
            Status = app.Status.ToString(),
            CreatedAt = app.CreatedAt,
            UpdatedAt = app.UpdatedAt
        };
    }

}
