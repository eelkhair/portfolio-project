using System;
using System.Security.Claims;
using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using JobApi.Application.Interfaces;
using JobApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application.Commands.Jobs;

public class CreateJobCommand(ClaimsPrincipal user, ILogger logger, Job job, Guid companyUId)
    : BaseCommand<Job>(user, logger), ICommand<Job>
{
    public Job Job { get; } = job;
    public Guid CompanyUId { get; } = companyUId;
}

public class CreateJobCommandHandler(IJobDbContext context) :BaseCommandHandler(context), IRequestHandler<CreateJobCommand, Job>
{
    public async Task<Job> HandleAsync(CreateJobCommand request, CancellationToken cancellationToken)
    {  
        var company = await Context.Companies.SingleAsync(c=> c.UId == request.CompanyUId, cancellationToken);
        request.Job.CompanyId = company.Id;
        request.Job.Company = company;
        Context.Jobs.Add(request.Job);
        await Context.SaveChangesAsync(request.User, cancellationToken);
        return request.Job;
    }
}