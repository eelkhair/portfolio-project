using System.Diagnostics;

namespace JobBoard.AI.Application.Interfaces.Observability;

public interface IActivityFactory
{
    Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext = default);
}