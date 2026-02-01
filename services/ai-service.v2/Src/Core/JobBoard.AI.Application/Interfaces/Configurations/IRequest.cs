namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IRequest<TResult>
{
    public string UserId { get; set; }
}
