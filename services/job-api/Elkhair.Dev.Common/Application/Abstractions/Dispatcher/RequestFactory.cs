using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elkhair.Dev.Common.Application.Abstractions.Dispatcher;

public class RequestFactory(IServiceProvider serviceProvider) : IRequestFactory
{
    public TRequest Create<TRequest>(params object[] args) where TRequest : class
    {
        var user = serviceProvider.GetRequiredService<ClaimsPrincipal>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(TRequest));

        var ctor = typeof(TRequest).GetConstructors().SingleOrDefault()
                   ?? throw new InvalidOperationException($"No public constructor for {typeof(TRequest).Name}");

        var ctorParams = ctor.GetParameters();
        var resolvedArgs = new List<object>();

        foreach (var param in ctorParams)
        {
            if (param.ParameterType == typeof(ClaimsPrincipal))
                resolvedArgs.Add(user);
            else if (param.ParameterType == typeof(ILogger))
                resolvedArgs.Add(logger);
            else
            {
                var match = args.FirstOrDefault(a => param.ParameterType.IsInstanceOfType(a));
                if (match == null)
                    throw new ArgumentException($"No argument for parameter {param.Name} of type {param.ParameterType.Name}");
                resolvedArgs.Add(match);
            }
        }

        return Activator.CreateInstance(typeof(TRequest), resolvedArgs.ToArray()) as TRequest
               ?? throw new InvalidOperationException($"Could not create instance of {typeof(TRequest).Name}");
    }
}