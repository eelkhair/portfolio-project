using JobBoard.AI.Application.Actions.Base;

namespace JobBoard.AI.API.Actions.Test;

public class TestCommand : BaseCommand<TestCommandResponse>;

public class TestCommandResponse
{
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
