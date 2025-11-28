using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;

namespace JobBoard.Application.Actions.Outbox;

public class ProcessOutboxMessageCommand : BaseCommand<bool>, INoTransaction
{
    
}

public class ProcessOutboxMessage(IHandlerContext context) : BaseCommandHandler(context),
    IHandler<ProcessOutboxMessageCommand, bool>
{
    public async Task<bool> HandleAsync(ProcessOutboxMessageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

