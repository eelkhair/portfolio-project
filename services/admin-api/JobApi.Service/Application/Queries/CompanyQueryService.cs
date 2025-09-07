using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobApi.Application.Queries.Interfaces;
using JobAPI.Contracts.Models.Companies.Responses;
using JobApi.Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application.Queries;

public class CompanyQueryService(IJobDbContext jobDbContext, ILogger<CompanyQueryService> logger)
: ICompanyQueryService
{
    public async Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct)
    {
        logger.LogInformation("hello");
       
        var client = new DaprClientBuilder().Build();
        var users = await client.InvokeMethodAsync<object>(HttpMethod.Get, "user-api", "users", ct);

       
        Console.WriteLine(users);
        
        var companies = await jobDbContext.Companies.AsNoTracking().ToListAsync(ct);
        return companies.Adapt<List<CompanyResponse>>();
        
    }
}