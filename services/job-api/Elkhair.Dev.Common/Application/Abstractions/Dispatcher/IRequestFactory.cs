namespace Elkhair.Dev.Common.Application.Abstractions.Dispatcher;

public interface IRequestFactory
{
    TRequest Create<TRequest>(params object[] args) where TRequest : class;
}