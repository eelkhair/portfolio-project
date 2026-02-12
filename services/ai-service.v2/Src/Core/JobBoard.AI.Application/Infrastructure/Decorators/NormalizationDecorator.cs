using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts.RewriteItem;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Infrastructure.Decorators;

public sealed class NormalizationCommandHandlerDecorator<TRequest, TResult>(
    IHandler<TRequest, TResult> decorated,
    ILogger<TRequest> logger,
    IActivityFactory activityFactory)
    : IHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Normalizing request {Request}...", typeof(TRequest).Name);
        using (var activity = activityFactory.StartActivity(
                   $"{typeof(TRequest).Name}.normalize",
                   ActivityKind.Internal))
        {
            Normalize(request, activity);
        }
        logger.LogInformation("Normalized request {Request}", typeof(TRequest).Name);
        return await decorated.HandleAsync(request, cancellationToken);
    }

    private static void Normalize(TRequest request, Activity? activity)
    {
        if (request is RewriteItemCommand rewrite)
        {
            NormalizeRewriteItem(rewrite, activity);
        }

        // Future:
        // if (request is ChatStreamCommand chat) { ... }
    }

    private static void NormalizeRewriteItem(
        RewriteItemCommand command,
        Activity? activity)
    {
        var req = command.Request;

        // Core
        req.Value = req.Value.Trim();

        // Context
        if (req.Context is not null)
        {
            req.Context.Title = req.Context.Title?.Trim();
            req.Context.AboutRole = req.Context.AboutRole?.Trim();
            req.Context.CompanyName = req.Context.CompanyName?.Trim();

            req.Context.Responsibilities =
                req.Context.Responsibilities?
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();

            req.Context.Qualifications =
                req.Context.Qualifications?
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();
        }

        // Style
        if (req.Style is not null)
        {
            req.Style.Audience = req.Style.Audience?.Trim();
            req.Style.Language = req.Style.Language?.Trim().ToLowerInvariant();

            req.Style.MaxWords = Clamp(req.Style.MaxWords, 10, 2_000);
            req.Style.NumParagraphs = Clamp(req.Style.NumParagraphs, 1, 4);
            req.Style.BulletsPerSection = Clamp(req.Style.BulletsPerSection, 3, 12);

            req.Style.AvoidPhrases =
                req.Style.AvoidPhrases?
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 1)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }

        activity?.SetTag("ai.normalized", true);
        activity?.SetTag("ai.normalized.request", nameof(RewriteItemCommand));
    }

    private static int? Clamp(int? value, int min, int max)
    {
        if (!value.HasValue) return null;
        return Math.Min(Math.Max(value.Value, min), max);
    }
}