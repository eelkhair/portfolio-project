using System.Diagnostics;
using OpenTelemetry;

namespace Elkhair.Common.Observability.Observability;

/// <summary>
/// OpenTelemetry span processor that hashes PII tag values (email, phone, first/last/full/user
/// name) before the span is exported. Runs on both <c>OnStart</c> and <c>OnEnd</c> so downstream
/// span-sampling / batch-exporter pipelines see the scrubbed values.
///
/// Registered alongside the other processors in <c>AddOpenTelemetryServices</c>.
/// </summary>
public sealed class PiiScrubbingSpanProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity data) => Scrub(data);

    public override void OnEnd(Activity data) => Scrub(data);

    private static void Scrub(Activity activity)
    {
        // Activity.TagObjects is the canonical tag collection. Activity.SetTag overwrites by key,
        // so reassigning is safe and cheap even if nothing matches.
        List<KeyValuePair<string, object?>>? toReplace = null;
        foreach (var tag in activity.TagObjects)
        {
            if (!PiiHasher.IsPiiKey(tag.Key))
                continue;
            toReplace ??= new List<KeyValuePair<string, object?>>();
            toReplace.Add(tag);
        }

        if (toReplace is null)
            return;

        foreach (var tag in toReplace)
        {
            var hashed = PiiHasher.Hash(tag.Value);
            activity.SetTag(tag.Key, hashed);
        }
    }
}
