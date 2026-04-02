using System.Text.Json;

namespace AdminApi.Mcp.Tools;

internal static class Json
{
    internal static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
