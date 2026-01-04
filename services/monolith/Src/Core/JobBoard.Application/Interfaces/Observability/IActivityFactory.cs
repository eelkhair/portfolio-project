using System.Diagnostics;

namespace JobBoard.Application.Interfaces.Observability;

public interface IActivityFactory
{
    Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext = default);
}