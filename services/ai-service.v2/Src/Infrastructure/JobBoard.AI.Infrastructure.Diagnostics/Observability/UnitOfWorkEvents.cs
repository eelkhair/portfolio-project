using JobBoard.AI.Application.Interfaces.Observability;

namespace JobBoard.AI.Infrastructure.Diagnostics.Observability;

public class UnitOfWorkEvents : IUnitOfWorkEvents
{
    private readonly List<Func<Task>> _actions = [];

    public void Enqueue(Func<Task> action)
    {
        _actions.Add(action);
    }

    public async Task ExecuteAndClearAsync()
    {
        foreach (var action in _actions)
        {
            await action();
        }
        _actions.Clear();
    }

    public void Clear()
    {
        _actions.Clear();
    }
}