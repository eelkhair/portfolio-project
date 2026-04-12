using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Applications.Admin;

public class UpdateBatchApplicationStatusCommand(List<Guid> applicationIds, string status)
    : BaseCommand<List<AdminApplicationListItem>>
{
    public List<Guid> ApplicationIds { get; } = applicationIds;
    public string Status { get; } = status;
}

public class UpdateBatchApplicationStatusCommandHandler(
    IJobBoardDbContext context,
    ILogger<UpdateBatchApplicationStatusCommandHandler> logger)
    : IHandler<UpdateBatchApplicationStatusCommand, List<AdminApplicationListItem>>
{
    public async Task<List<AdminApplicationListItem>> HandleAsync(
        UpdateBatchApplicationStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ApplicationStatus>(command.Status, true, out var newStatus))
            throw new FluentValidation.ValidationException($"Invalid status: {command.Status}");

        var apps = await context.JobApplications
            .Include(a => a.Job).ThenInclude(j => j.Company)
            .Include(a => a.User)
            .Where(a => command.ApplicationIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        foreach (var app in apps)
            app.SetStatus(newStatus);

        await context.SaveChangesAsync(command.UserId, cancellationToken);

        logger.LogInformation("Batch updated {Count} applications to {Status}",
            apps.Count, newStatus);

        return apps.Select(a => new AdminApplicationListItem
        {
            Id = a.Id,
            ApplicantName = $"{a.User.FirstName} {a.User.LastName}".Trim(),
            ApplicantEmail = a.User.Email,
            JobId = a.Job.Id,
            JobTitle = a.Job.Title,
            CompanyName = a.Job.Company.Name,
            Status = a.Status.ToString(),
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();
    }
}
