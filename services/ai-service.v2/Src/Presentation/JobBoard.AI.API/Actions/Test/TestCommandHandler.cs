using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.API.Actions.Test;

public class TestCommandHandler(IHandlerContext handlerContext)
    : BaseCommandHandler(handlerContext), IHandler<TestCommand, TestCommandResponse>
{
    public Task<TestCommandResponse> HandleAsync(TestCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Test command executed");

        return Task.FromResult(new TestCommandResponse
        {
            Message = "AI Service is running! Foundation is ready for AI features.",
            Timestamp = DateTime.UtcNow
        });
    }
}
