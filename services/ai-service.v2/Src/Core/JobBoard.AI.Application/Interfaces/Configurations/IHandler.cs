namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IHandler<in TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
