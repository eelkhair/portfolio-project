namespace JobBoard.Application.Interfaces.Observability;

public interface IUnitOfWorkEvents
{
    /// <summary>
    /// Adds an action to the queue.
    /// </summary>
    void Enqueue(Func<Task> action);

    /// <summary>
    /// Executes all queued actions and then clears the queue.
    /// </summary>
    Task ExecuteAndClearAsync();

    /// <summary>
    /// Clears the queue without executing any actions. Used for rollbacks.
    /// </summary>
    void Clear();
}