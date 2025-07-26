using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Elkhair.Dev.Common.Application.Abstractions.Dispatcher;

public abstract class BaseQuery<TResponse>(ClaimsPrincipal user, ILogger logger) : IRequest<TResponse>
{
    [JsonIgnore]
    public ClaimsPrincipal User { get; } = user;

    [JsonIgnore]
    public ILogger Logger { get; } = logger;
}