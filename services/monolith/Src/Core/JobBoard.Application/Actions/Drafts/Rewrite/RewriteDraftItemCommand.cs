using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.Rewrite;

public class RewriteDraftItemCommand : BaseCommand<DraftRewriteResponse>, INoTransaction
{
    public DraftItemRewriteRequest DraftItemRewriteRequest { get; set; } = new();
}

public class RewriteDraftItemCommandHandler(IHandlerContext handlerContext, IAiServiceClient client) : BaseCommandHandler(handlerContext),
    IHandler<RewriteDraftItemCommand, DraftRewriteResponse>
{
    public async Task<DraftRewriteResponse> HandleAsync(RewriteDraftItemCommand request, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("field", request.DraftItemRewriteRequest.Field);
        Logger.LogInformation("Calling the AI Service to rewrite {Field} ...", request.DraftItemRewriteRequest.Field);
       
        var drafts = await client.RewriteItem(request.DraftItemRewriteRequest, cancellationToken);
        
        Logger.LogInformation("Rewrite completed");
        return drafts;
    }
}

