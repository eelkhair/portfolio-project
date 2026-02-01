using System.Text.Json.Serialization;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.Base;

public abstract class BaseCommand<TResponse> : IRequest<TResponse>
{
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
}
