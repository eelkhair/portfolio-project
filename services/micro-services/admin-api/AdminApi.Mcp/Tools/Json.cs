using System.Text.Json;

namespace AdminApi.Mcp.Tools;

internal static class Json
{
    internal static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static (bool Ok, Guid Value, string? Error) ParseGuid(string? input, string paramName)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, Guid.Empty, Serialize(new { error = $"{paramName} is required." }, Opts));
        if (Guid.TryParse(input, out var guid))
            return (true, guid, null);
        return (false, Guid.Empty, Serialize(new { error = $"Invalid {paramName}: '{input}'. Expected a GUID from company_list Id field, not a name." }, Opts));
    }

    private static string Serialize<T>(T value, JsonSerializerOptions opts) => JsonSerializer.Serialize(value, opts);
}
