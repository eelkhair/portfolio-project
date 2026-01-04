using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Elkhair.Common.Observability.Observability;

public class DaprInternalSpanFilter : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.DisplayName.Contains("Dapr/GetConfiguration") ||
            activity.DisplayName.Contains("Dapr/SubscribeConfiguration"))
        {
            // Drop the span completely
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}

public sealed class DaprConfigSampler : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        // This is the span name you see in Jaeger:
        // /dapr.proto.runtime.v1.Dapr/GetConfiguration
        if (parameters.Name.Contains("dapr.proto.runtime.v1.Dapr/GetConfiguration",
                StringComparison.OrdinalIgnoreCase))
        {
            // Completely drop these spans
            return new SamplingResult(SamplingDecision.Drop);
        }

        // Everything else → keep as normal
        return new SamplingResult(SamplingDecision.RecordAndSample);
    }
    
}