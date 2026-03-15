using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.IntegrationEvents.Draft;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Jobs.Create;

public class CreateJobCommand(CreateJobRequest request) : BaseCommand<JobResponse>
{
    public CreateJobRequest Request { get; set;} = request;
}

public class CreateJobCommandHandler(IHandlerContext context,
    ICompanyRepository companyRepository,
    IJobRepository jobRepository) : 
    BaseCommandHandler(context), IHandler<CreateJobCommand, JobResponse>
{
    public async Task<JobResponse> HandleAsync(CreateJobCommand command, CancellationToken cancellationToken)
    {
        command.Request.SetActivityTagsForJob(Activity.Current);
        Logger.LogInformation("Creating job with job title: {JobTitle} for company: {CompanyId}..", command.Request.Title, command.Request.CompanyUId);
       
        var (id ,uId) = await Context.GetNextValueFromSequenceAsync(typeof(Job), cancellationToken);
        
        var company = await companyRepository.GetCompanyById(command.Request.CompanyUId, cancellationToken);
        
        var job = command.Request.ToJobEntity(uId, id, company.InternalId);
        
        await jobRepository.AddAsync(job, cancellationToken);
        
        // Delete draft if requested
        if (command.Request.DeleteDraft && !string.IsNullOrWhiteSpace(command.Request.DraftId)
            && Guid.TryParse(command.Request.DraftId, out var draftGuid))
        {
            var draftsDbSet = ((IJobBoardQueryDbContext)Context).Drafts;
            var draft = await draftsDbSet.FirstOrDefaultAsync(d => d.Id == draftGuid, cancellationToken);
            if (draft is not null)
            {
                draftsDbSet.Remove(draft);
                var draftDeletedEvent = new DraftDeletedV1Event(draft.Id, command.Request.CompanyUId) { UserId = command.UserId };
                await OutboxPublisher.PublishAsync(draftDeletedEvent, cancellationToken);
                Logger.LogInformation("Draft {DraftId} will be deleted with job creation", command.Request.DraftId);
            }
        }

        var integrationEvent = command.Request.ToIntegrationEvent(uId);
        await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);
        
        var parentActivity = Activity.Current;
        UnitOfWorkEvents.Enqueue(() =>
        {
            parentActivity?.SetTag("CompanyUId", command.Request.CompanyUId.ToString());
            parentActivity?.SetTag("JobUId", job.Id);
            parentActivity?.SetTag("JobTitle", command.Request.Title);  
            parentActivity?.SetTag("JobType", command.Request.JobType.ToString());
            parentActivity?.SetTag("Location", command.Request.Location);
            parentActivity?.SetTag("CompanyName", company.Name);
   
            Logger.LogInformation(
                "Successfully created job {JobTitle} ({JobUId}) for company {CompanyName} ({CompanyUId})",
                command.Request.Title,
                job.Id,
                company.Name,
                command.Request.CompanyUId);

            return Task.CompletedTask;
        });
        return new JobResponse
        {
            Id = job.Id,
            CompanyUId = command.Request.CompanyUId,
            Title = command.Request.Title,
            JobType = command.Request.JobType,
            Location = command.Request.Location,
            CompanyName = company.Name,
            AboutRole = command.Request.AboutRole,
            SalaryRange = command.Request.SalaryRange,
            Responsibilities = command.Request.Responsibilities,
            Qualifications = command.Request.Qualifications
        };
    }
}