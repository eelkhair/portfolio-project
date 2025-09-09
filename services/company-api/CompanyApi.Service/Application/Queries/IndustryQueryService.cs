using System.Diagnostics;
using CompanyApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Industries.Responses;
using CompanyApi.Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Application.Queries;

public class IndustryQueryService(ICompanyDbContext companyDbContext, ILogger<IndustryQueryService> logger, ActivitySource activitySource) : IIndustryQueryService
{
    public async Task<List<IndustryResponse>> ListAsync(CancellationToken ct)
    { 
        var activity = activitySource.StartActivity("ListIndustries");
        try
        {
            logger.LogInformation("Fetching Industries");
            var industries = await companyDbContext.Industries.AsNoTracking().ToListAsync(ct);

            activity?.SetTag("industries.count", industries.Count);

            activity?.Stop();
            return industries.Adapt<List<IndustryResponse>>();
        }catch (OperationCanceledException e)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "canceled");
            logger.LogError(e, "Fetch industries canceled");
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception",
                tags: new ActivityTagsCollection
                {
                    ["exception.type"] = ex.GetType().FullName!,
                    ["exception.message"] = ex.Message
                }));

            logger.LogError(ex, "Failed to fetch industries");
            throw;
        }
    }
}