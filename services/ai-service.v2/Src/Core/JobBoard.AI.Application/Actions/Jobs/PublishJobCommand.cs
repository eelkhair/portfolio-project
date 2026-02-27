using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.Jobs;

public class PublishJobCommand(EventDto<PublishedJobEvent> @event) : BaseCommand<Unit>
{
    public EventDto<PublishedJobEvent> Event { get; set; } = @event;
}

public class PublishJobCommandHandler(IHandlerContext context) : BaseCommandHandler(context),
    IHandler<PublishJobCommand, Unit>
{
    public async Task<Unit> HandleAsync(PublishJobCommand request, CancellationToken cancellationToken)
    {
        return Unit.Value;
    }
}