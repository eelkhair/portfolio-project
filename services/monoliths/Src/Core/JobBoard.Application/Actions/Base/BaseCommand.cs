using System.Text.Json.Serialization;
using JobBoard.Application.Interfaces.Configurations;

namespace JobBoard.Application.Actions.Base;

// ReSharper disable once UnusedTypeParameter
public abstract class BaseCommand<TResponse> : IRequest<TResponse>
{
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
}
