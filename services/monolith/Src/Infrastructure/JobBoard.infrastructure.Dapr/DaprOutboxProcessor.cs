using Dapr.Client;
using JobBoard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.infrastructure.Dapr;

public class DaprOutboxProcessor(
    DaprClient daprClient,
    IOutboxDbContext outboxDbContext,
    ILogger<DaprOutboxProcessor> logger
)
{
    public async Task<int> PublishPendingEventsAsync(CancellationToken cancellationToken)
    {
        var pending = await outboxDbContext.OutboxMessages
            .Where(c=> c.ProcessedAt == null).Take(25).ToListAsync(cancellationToken);

        return 0;
    }
}