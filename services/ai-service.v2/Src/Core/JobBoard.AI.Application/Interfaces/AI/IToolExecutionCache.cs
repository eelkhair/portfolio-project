namespace JobBoard.AI.Application.Interfaces.AI;

public interface IToolExecutionCache
{
    bool TryGet(string key, out object? value);
    void Set(string key, object value);
}