using JobBoard.Application.Actions.Base;

namespace JobBoard.Application.Interfaces.Configurations;
public interface IApplicationOrchestrator
{
    Task<TResult> ExecuteCommandAsync<TResult>(BaseCommand<TResult> command, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteQueryAsync<TResult>(BaseQuery<TResult> query, CancellationToken cancellationToken = default);
}
