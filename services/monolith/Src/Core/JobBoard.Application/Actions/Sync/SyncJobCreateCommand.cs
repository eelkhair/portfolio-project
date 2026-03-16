using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Sync;

public class SyncJobCreateCommand : BaseCommand<Unit>
{
    public Guid JobId { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AboutRole { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? SalaryRange { get; set; }
    public string JobType { get; set; } = string.Empty;
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
}

/// <summary>
/// Reverse-sync handler: creates a Job from a microservice event.
/// Does NOT call OutboxPublisher to prevent infinite sync loops.
/// </summary>
public class SyncJobCreateCommandHandler(
    IHandlerContext handlerContext,
    ICompanyRepository companyRepository,
    IJobRepository jobRepository)
    : BaseCommandHandler(handlerContext), IHandler<SyncJobCreateCommand, Unit>
{
    public async Task<Unit> HandleAsync(SyncJobCreateCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("sync.job.id", command.JobId);
        Activity.Current?.SetTag("sync.job.companyId", command.CompanyId);
        Logger.LogInformation("Reverse-sync: creating job {JobTitle} ({JobId}) for company {CompanyId}",
            command.Title, command.JobId, command.CompanyId);

        // Idempotency: skip if job already exists
        var queryContext = (IJobBoardQueryDbContext)Context;
        var exists = await queryContext.Jobs
            .AnyAsync(j => j.Id == command.JobId, cancellationToken);

        if (exists)
        {
            Logger.LogInformation("Reverse-sync: job {JobId} already exists, skipping", command.JobId);
            Activity.Current?.SetTag("sync.job.skipped", true);
            return Unit.Value;
        }

        var (internalId, _) = await Context.GetNextValueFromSequenceAsync(typeof(Job), cancellationToken);
        var company = await companyRepository.GetCompanyById(command.CompanyId, cancellationToken);

        if (!Enum.TryParse<JobType>(command.JobType, ignoreCase: true, out var jobType))
        {
            jobType = JobType.FullTime; // Sensible default
            Logger.LogWarning("Reverse-sync: unknown job type '{JobType}', defaulting to FullTime", command.JobType);
        }

        var job = Job.Create(new JobInput
        {
            Title = command.Title,
            AboutRole = command.AboutRole,
            Location = command.Location,
            SalaryRange = command.SalaryRange,
            JobType = jobType,
            CompanyId = company.InternalId,
            Responsibilities = command.Responsibilities,
            Qualifications = command.Qualifications,
            InternalId = internalId,
            UId = command.JobId
        });

        await jobRepository.AddAsync(job, cancellationToken);

        // No OutboxPublisher.PublishAsync() — prevents reverse-sync → forward-sync loop
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Activity.Current?.SetTag("sync.job.created", true);
        Logger.LogInformation("Reverse-sync: created job {JobTitle} ({JobId})", command.Title, command.JobId);

        return Unit.Value;
    }
}
