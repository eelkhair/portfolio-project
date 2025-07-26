using System.Security.Claims;
using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using JobApi.Application.Interfaces;
using JobApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application.Queries.Companies;

public class ListCompaniesQuery(ClaimsPrincipal user, ILogger logger) : BaseQuery<List<Company>>(user, logger);

public class ListCompaniesHandler(IJobDbContext context)
    : BaseQueryHandler(context), IRequestHandler<ListCompaniesQuery, List<Company>>
{
    public async Task<List<Company>> HandleAsync(ListCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await Context.Companies.ToListAsync(cancellationToken);
    }
}