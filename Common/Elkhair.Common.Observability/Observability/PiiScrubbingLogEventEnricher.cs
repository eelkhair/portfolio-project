using Serilog.Core;
using Serilog.Events;

namespace Elkhair.Common.Observability.Observability;

/// <summary>
/// Walks the properties of every <see cref="LogEvent"/> and replaces values whose property
/// name indicates PII (email, phone, first/last/full/user name) with a stable SHA-256 hash.
///
/// Must be registered AFTER <see cref="OpenTelemetryActivityEnricher"/> so it also catches
/// the <c>otel.tag.*</c> properties that enricher copies from <c>Activity.Current.Tags</c>
/// (for example <c>otel.tag.email</c>, <c>otel.tag.signup.email</c>).
/// </summary>
public sealed class PiiScrubbingLogEventEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Snapshot the keys first — we're going to replace entries and cannot mutate during
        // enumeration of LogEvent.Properties.
        List<string>? keysToScrub = null;
        foreach (var kvp in logEvent.Properties)
        {
            if (!PiiHasher.IsPiiKey(kvp.Key))
                continue;
            keysToScrub ??= new List<string>();
            keysToScrub.Add(kvp.Key);
        }

        if (keysToScrub is null)
            return;

        foreach (var key in keysToScrub)
        {
            var original = logEvent.Properties[key];
            var hashed = HashScalar(original);
            if (hashed is not null)
                logEvent.AddOrUpdateProperty(new LogEventProperty(key, hashed));
        }
    }

    private static ScalarValue? HashScalar(LogEventPropertyValue value)
    {
        // Only scrub scalar values. Structure/dictionary/sequence values under a PII key are
        // unusual and left untouched; if those become a concern, extend the walker here.
        if (value is ScalarValue scalar)
        {
            var hashed = PiiHasher.Hash(scalar.Value);
            return hashed is null ? null : new ScalarValue(hashed);
        }

        // Fall back to stringifying so at worst we store a hash of the rendered form instead of
        // the raw value.
        var rendered = value.ToString();
        var fallback = PiiHasher.Hash(rendered);
        return fallback is null ? null : new ScalarValue(fallback);
    }
}
