using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetPublicJobByIdQuery(Guid id) : BaseQuery<JobResponse>, IAnonymousRequest
{
    public Guid Id { get; } = id;
}

public class GetPublicJobByIdQueryHandler(IJobBoardQueryDbContext context, ILogger<GetPublicJobByIdQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetPublicJobByIdQuery, JobResponse>
{
    public async Task<JobResponse> HandleAsync(GetPublicJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await Context.Jobs
            .Where(j => j.Id == request.Id)
            .Select(job => new JobResponse
            {
                Id = job.Id,
                CompanyUId = job.Company.Id,
                Title = job.Title,
                JobType = job.JobType,
                Location = job.Location,
                CompanyName = job.Company.Name,
                AboutRole = job.AboutRole,
                SalaryRange = job.SalaryRange,
                Responsibilities = job.Responsibilities.Select(r => r.Value).ToList(),
                Qualifications = job.Qualifications.Select(q => q.Value).ToList(),
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return job ?? throw new NotFoundException("Job", request.Id);
    }
}
