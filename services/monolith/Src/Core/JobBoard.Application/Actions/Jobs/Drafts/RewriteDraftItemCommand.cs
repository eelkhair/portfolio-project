using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Jobs.Models;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Jobs.Drafts;

public class RewriteDraftItemCommand : BaseCommand<JobRewriteResponse>, INoTransaction
{
    public JobRewriteRequest JobRewriteRequest { get; set; } = new();
}

public class RewriteDraftItemCommandHandler(IHandlerContext handlerContext, IAiServiceClient client) : BaseCommandHandler(handlerContext),
    IHandler<RewriteDraftItemCommand, JobRewriteResponse>
{
    public async Task<JobRewriteResponse> HandleAsync(RewriteDraftItemCommand request, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("field", request.JobRewriteRequest.Field);
        Logger.LogInformation("Calling the AI Service to rewrite {Field} ...", request.JobRewriteRequest.Field);
       
        var drafts = await client.RewriteItem(request.JobRewriteRequest, cancellationToken);
        
        Logger.LogInformation("Rewrite completed");
        return drafts;
    }
}

