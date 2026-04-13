using System.Diagnostics;
using CompanyApi.Application.Queries.Interfaces;
using CompanyApi.Infrastructure.Data;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Application.Queries;

public partial class IndustryQueryService(ICompanyDbContext companyDbContext, ILogger<IndustryQueryService> logger, ActivitySource activitySource) : IIndustryQueryService
{
    public async Task<List<IndustryResponse>> ListAsync(CancellationToken ct)
    {
        var activity = activitySource.StartActivity("ListIndustries");
        try
        {
            LogFetchingIndustries(logger);
            var industries = await companyDbContext.Industries.AsNoTracking().ToListAsync(ct);

            activity?.SetTag("industries.count", industries.Count);

            LogIndustriesFetched(logger, industries.Count);
            activity?.Stop();
            return industries.Adapt<List<IndustryResponse>>();
        }
        catch (OperationCanceledException e)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "canceled");
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
            throw;
        }
    }

    [LoggerMessage(LogLevel.Information, "Fetching industries")]
    static partial void LogFetchingIndustries(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Industries fetched, returned {Count} industries")]
    static partial void LogIndustriesFetched(ILogger logger, int count);
}
