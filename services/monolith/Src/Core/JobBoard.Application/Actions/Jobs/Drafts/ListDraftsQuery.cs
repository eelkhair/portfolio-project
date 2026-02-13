using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Jobs.Drafts;

public class ListDraftsQuery: BaseQuery<List<JobDraftResponse>>
{
    public Guid CompanyId { get; set; }
}

public class ListDraftsQueryHandler(IJobBoardQueryDbContext context, ILogger<ListDraftsQueryHandler> logger, IAiServiceClient aiServiceClient)
    : BaseQueryHandler(context, logger), IHandler<ListDraftsQuery, List<JobDraftResponse>>
{
    public async Task<List<JobDraftResponse>> HandleAsync(ListDraftsQuery request, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("company.id", request.CompanyId);
        Logger.LogInformation("Fetching drafts for company {CompanyId} from the AI Service...", request.CompanyId);
       
        var drafts = await aiServiceClient.ListDrafts(request.CompanyId, cancellationToken);
        
        Logger.LogInformation("Fetched {DraftCount} drafts for company {CompanyId} from the AI Service", drafts.Count, request.CompanyId);
        Activity.Current?.SetTag("drafts.count", drafts.Count);
        return drafts;
    }
}