using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Drafts.RewriteItem;

public class RewriteItemCommand(RewriteItemRequest request) : BaseCommand<RewriteItemResponse>
{
    public RewriteItemRequest Request { get; } = request;
}

public sealed class RewriteItemCommandHandler(
    IHandlerContext context,
    IAiPrompt<RewriteItemRequest> aiPrompt,
    ICompletionService completionService,
    IActivityFactory activityFactory)
    : BaseCommandHandler(context),
        IHandler<RewriteItemCommand, RewriteItemResponse>
{
    public async Task<RewriteItemResponse> HandleAsync(
        RewriteItemCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            nameof(RewriteItemCommand),
            ActivityKind.Internal);

        var request = command.Request;

        activity?.SetTag("ai.operation", "rewrite_item");
        activity?.SetTag("rewrite.field", request.Field.ToString());
        activity?.SetTag("prompt.name", aiPrompt.Name);
        activity?.SetTag("prompt.version", aiPrompt.Version);

        Logger.LogInformation(
            "Rewriting {Field} item",
            request.Field);

        var userPrompt = aiPrompt.BuildUserPrompt(request);
        var systemPrompt = aiPrompt.BuildSystemPrompt();

        var response = await completionService
            .GetResponseAsync<RewriteItemResponse>(
                systemPrompt,
                userPrompt,
                aiPrompt.AllowTools,
                cancellationToken);

        Logger.LogInformation(
            "Rewrite completed for {Field}",
            request.Field);

        // Ensure response.field matches request.field
        response.Field = request.Field;

        return response;
    }
}

