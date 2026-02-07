using JobBoard.AI.Application.Actions.Base;

namespace JobBoard.AI.Application.Interfaces.Configurations;
public interface IApplicationOrchestrator
{
    Task<TResult> ExecuteCommandAsync<TResult>(BaseCommand<TResult> command, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteQueryAsync<TResult>(BaseQuery<TResult> query, CancellationToken cancellationToken = default);
}
