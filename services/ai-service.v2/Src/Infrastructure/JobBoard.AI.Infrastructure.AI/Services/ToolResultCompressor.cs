using System.Text.Json;
using JobBoard.AI.Application.Interfaces.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.Services;

/// <summary>
/// Replaces large tool result JSON in conversation history with compact summaries.
/// This runs BEFORE saving to Redis so future turns don't re-send massive payloads.
/// The current turn already saw the full result; only the history copy is trimmed.
/// </summary>
public class ToolResultCompressor(ILogger<ToolResultCompressor> logger) : IToolResultCompressor
{
    /// <summary>
    /// Results shorter than this (in characters) are left untouched.
    /// </summary>
    private const int MinCompressLength = 500;

    /// <summary>
    /// For array results, keep at most this many items in the compressed output.
    /// </summary>
    private const int MaxArrayPreviewItems = 3;

    /// <summary>
    /// Max characters for any single compressed result.
    /// </summary>
    private const int MaxCompressedLength = 600;

    public void CompressToolResults(List<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            for (var i = 0; i < message.Contents.Count; i++)
            {
                if (message.Contents[i] is not FunctionResultContent result) continue;

                var raw = result.Result?.ToString();
                if (string.IsNullOrEmpty(raw) || raw.Length <= MinCompressLength) continue;

                var compressed = CompressPayload(result.CallId, raw);

                message.Contents[i] = new FunctionResultContent(result.CallId, compressed);
            }
        }
    }

    private string CompressPayload(string? callId, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.Array => CompressArray(root),
                JsonValueKind.Object => CompressObject(root),
                _ => Truncate(json)
            };
        }
        catch (JsonException)
        {
            // Not valid JSON — just truncate
            logger.LogDebug("Tool result {CallId} is not valid JSON, truncating", callId);
            return Truncate(json);
        }
    }

    /// <summary>
    /// Array results (e.g. company_list, job_list): keep count + first N items preview.
    /// </summary>
    private static string CompressArray(JsonElement array)
    {
        var count = array.GetArrayLength();
        if (count == 0) return "[]";

        var preview = new List<string>();
        var idx = 0;

        foreach (var item in array.EnumerateArray())
        {
            if (idx >= MaxArrayPreviewItems) break;
            preview.Add(CompressObjectToSummary(item));
            idx++;
        }

        var remaining = count - MaxArrayPreviewItems;
        var result = $"[{count} items] [{string.Join(", ", preview)}]";

        if (remaining > 0)
            result += $" ...and {remaining} more";

        return result.Length > MaxCompressedLength ? result[..MaxCompressedLength] + "..." : result;
    }

    /// <summary>
    /// Object results (e.g. single create_company response): keep key scalar fields, drop nested arrays.
    /// </summary>
    private static string CompressObject(JsonElement obj)
    {
        var summary = CompressObjectToSummary(obj);
        return summary.Length > MaxCompressedLength ? summary[..MaxCompressedLength] + "..." : summary;
    }

    /// <summary>
    /// Extracts key scalar fields from a JSON object into a compact "key=value" string.
    /// Nested objects/arrays are summarized as counts.
    /// </summary>
    private static string CompressObjectToSummary(JsonElement obj)
    {
        if (obj.ValueKind != JsonValueKind.Object)
            return obj.ToString() ?? "";

        var parts = new List<string>();

        foreach (var prop in obj.EnumerateObject())
        {
            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.String:
                    var str = prop.Value.GetString() ?? "";
                    parts.Add(str.Length > 80
                        ? $"{prop.Name}=\"{str[..80]}...\""
                        : $"{prop.Name}=\"{str}\"");
                    break;

                case JsonValueKind.Number:
                    parts.Add($"{prop.Name}={prop.Value}");
                    break;

                case JsonValueKind.True or JsonValueKind.False:
                    parts.Add($"{prop.Name}={prop.Value}");
                    break;

                case JsonValueKind.Array:
                    parts.Add($"{prop.Name}=[{prop.Value.GetArrayLength()} items]");
                    break;

                case JsonValueKind.Object:
                    // Skip nested objects to keep it flat
                    break;

                case JsonValueKind.Null:
                    // Skip nulls
                    break;
            }
        }

        return $"{{{string.Join(", ", parts)}}}";
    }

    private static string Truncate(string text)
    {
        return text.Length > MaxCompressedLength
            ? text[..MaxCompressedLength] + "...(truncated)"
            : text;
    }
}
