using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.AI.Application.Actions.Jobs.Publish;

public class PublishJobCommand(EventDto<PublishedJobEvent> @event) : BaseCommand<Unit>, ISystemCommand
{
    public EventDto<PublishedJobEvent> Event { get; set; } = @event;
}

public class PublishJobCommandHandler(IHandlerContext context, 
    IAiDbContext dbContext, 
    IEmbeddingService embeddingService, 
    IActivityFactory activityFactory) : BaseCommandHandler(context),
    IHandler<PublishJobCommand, Unit>
{
    public async Task<Unit> HandleAsync(PublishJobCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("PublishJobCommandHandler.HandleAsync", ActivityKind.Internal);
        
        var job = request.Event.Data;
        
        activity?.SetTag("job.id", job.UId);
        
        var embeddingText = BuildEmbeddingText(job);
        
        activity?.SetTag("job.title", job.Title);
        var vector = await embeddingService.GenerateEmbeddingsAsync(
            embeddingText,
            cancellationToken);

        var jobEmbedding = new JobEmbedding(
            jobId: job.UId,
            vector: new EmbeddingVector(vector),
            provider: new ProviderName("openai.embedding"),
            model: new ModelName("text-embedding-3-small")
        );
        
        await dbContext.JobEmbeddings.AddAsync(jobEmbedding, cancellationToken);

        if (!string.IsNullOrWhiteSpace(job.DraftId))
        {
            if (job.DeleteDraft)
            {
                var draft = await dbContext.Drafts.FirstOrDefaultAsync(c=> c.Id.ToString()== job.DraftId, cancellationToken);
                if (draft != null)
                {
                    dbContext.Drafts.Remove(draft);
                }
                Activity.Current?.SetTag("job.draft.delete", true);
            }
            else
            {
                Activity.Current?.SetTag("job.draft.delete", false);
            }

            Activity.Current?.SetTag("job.draft.id", job.DraftId);
        }
        else
        {
            Activity.Current?.SetTag("job.draft.delete", false);
            Activity.Current?.SetTag("job.draft.id", null);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
    
    private static string BuildEmbeddingText(PublishedJobEvent job)
    {
        return $"""
                Job Title: {job.Title}
                Company: {job.CompanyName}
                Location: {job.Location}
                Job Type: {job.JobType}

                About the Role:
                {job.AboutRole}

                Responsibilities:
                {string.Join("\n", job.Responsibilities)}

                Qualifications:
                {string.Join("\n", job.Qualifications)}
                """;
    }
}

