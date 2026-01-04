using JobBoard.Application.Interfaces.Configurations;

namespace JobBoard.Application.Actions.Base;

public abstract class BaseQuery<TResult>: IRequest<TResult>
{
    public string UserId { get; set; }= string.Empty;
}