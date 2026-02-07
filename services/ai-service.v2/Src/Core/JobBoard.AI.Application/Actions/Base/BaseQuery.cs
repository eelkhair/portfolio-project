using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.Base;

public abstract class BaseQuery<TResult>: IRequest<TResult>
{
    public string UserId { get; set; }= string.Empty;
}