using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;

namespace JobBoard.Application.Actions;

public class TestCommand : BaseCommand<string>
{
    
}

public class TestCommandHandler(IHandlerContext context)
    : BaseCommandHandler(context), IHandler<TestCommand, string>
{
    public Task<string> HandleAsync(TestCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}