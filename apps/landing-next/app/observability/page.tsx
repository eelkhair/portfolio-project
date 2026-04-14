import type { Metadata } from "next";
import { CaseStudyLayout, CsSection, CsDecisionGrid, CsTradeoffList, CsPlaceholder } from "../components/CaseStudyLayout";

export const metadata: Metadata = {
  title: "Observability",
  description: "End-to-end distributed tracing, structured logging, and dashboards across 16 services using OpenTelemetry, Jaeger, Grafana, and Seq.",
};

const toc = [
  { href: "#problem", label: "Problem" },
  { href: "#solution", label: "Solution" },
  { href: "#architecture", label: "Architecture" },
  { href: "#user-view", label: "What You See" },
  { href: "#behind-scenes", label: "Behind the Scenes" },
  { href: "#decisions", label: "Key Decisions" },
  { href: "#tradeoffs", label: "Tradeoffs" },
];

export default function ObservabilityPage() {
  return (
    <CaseStudyLayout
      title="Observability"
      summary="End-to-end distributed tracing, structured logging, and dashboards across 16 services &mdash; so every request is debuggable from browser to database."
      toc={toc}
      prevLink={{ href: "/ai-orchestration", label: "Previous: AI Orchestration" }}
      nextLink={{ href: "/portfolio", label: "Back to Portfolio" }}
    >
      <CsSection id="problem">
        <h2>The Problem</h2>
        <p>A single request in this system can touch five or more services: gateway, monolith or microservices, AI service, MCP server, and background processors. Without correlated telemetry, a failure in one service looks like a mystery in another.</p>
        <p>Most portfolio projects treat logging as an afterthought &mdash; a few <code>console.log</code> statements or uncorrelated log files. That doesn&apos;t reflect how production distributed systems are actually operated.</p>
      </CsSection>

      <CsSection id="solution" alt>
        <h2>The Solution</h2>
        <p>Every service is instrumented with <strong>OpenTelemetry</strong>. Traces follow the <strong>W3C TraceContext</strong> standard, propagating through HTTP calls, Dapr pub/sub messages, and background processors. A single TraceId follows a request from the Angular frontend through the gateway, into whichever backend path is active, through messaging, and back.</p>
        <p><strong>Jaeger</strong> visualizes distributed traces. <strong>Grafana</strong> shows API latency, error rates, and throughput. <strong>Seq</strong> provides structured log search with TraceId correlation. <strong>Health checks</strong> monitor liveness and readiness for every service.</p>
        <CsPlaceholder text="Screenshot: Full end-to-end Jaeger trace" caption="A Jaeger trace showing a complete request flow from gateway through multiple backend services" />
      </CsSection>

      <CsSection id="architecture">
        <h2>Architecture</h2>
        <p>Each .NET service configures OpenTelemetry through a shared extension method. The OTLP exporter sends traces and metrics to a centralized OpenTelemetry Collector, which fans out to Jaeger (traces) and Grafana (metrics via Prometheus).</p>
        <p>Trace context propagation through messaging is the hard part. When a service publishes a Dapr event, the W3C <code>traceparent</code> header is included in the message metadata. The consuming service extracts it and creates a child span, maintaining the trace across async boundaries.</p>
        <CsPlaceholder text="Screenshot: Trace showing context propagation through Dapr pub/sub" caption="A trace spanning Dapr pub/sub &mdash; the trace context propagates from producer through RabbitMQ to consumer" />
      </CsSection>

      <CsSection id="user-view" alt>
        <h2>What You See</h2>
        <p>Click &quot;Create Company&quot; in the admin app. The API response includes a <code>TraceId</code> header. Click the Jaeger link in the admin toolbar. You&apos;ll see the full trace: gateway to monolith, outbox dispatch, connector saga fanning out to three microservices, reverse connector syncing back.</p>
        <p>Open Grafana and check the API dashboard. You&apos;ll see request latency percentiles, error rates by endpoint, and throughput over time. Filter by the TraceId to correlate a specific request&apos;s metrics with its trace. Open Seq and paste the same TraceId &mdash; every structured log entry from every service involved in that request appears, correlated and searchable.</p>
        <div className="cs-evidence-grid">
          <CsPlaceholder text="Screenshot: Grafana dashboard" caption="Grafana dashboard with request latency percentiles, error rates, and throughput across services" />
          <CsPlaceholder text="Screenshot: Seq structured logs filtered by TraceId" caption="Seq log search filtered by TraceId, showing correlated entries across multiple services" />
        </div>
      </CsSection>

      <CsSection id="behind-scenes">
        <h2>Behind the Scenes</h2>
        <p>Custom <code>Activity</code> spans wrap the CQRS decorator pipeline. Instead of generic span names like &quot;POST /api/companies&quot;, the traces show business-meaningful names like &quot;CreateCompanyCommand&quot; and &quot;PublishOutboxEvents&quot;. This makes traces readable without needing to map HTTP paths back to domain operations.</p>
        <p>The health check system separates concerns: <strong>liveness</strong> checks confirm the process is running, <strong>readiness</strong> checks confirm dependencies (database, Redis, RabbitMQ) are reachable. The health dashboard aggregates all services and shows which are ready to serve traffic.</p>
        <p>The admin Angular app includes the TraceId from API responses in the toolbar, with a direct link to Jaeger. This closes the loop &mdash; a developer or visitor can go from a UI action to a full distributed trace in one click.</p>
      </CsSection>

      <CsSection id="decisions" alt>
        <h2>Key Decisions</h2>
        <CsDecisionGrid decisions={[
          { title: "OpenTelemetry over vendor-specific SDKs", content: "<p><strong>Why:</strong> OpenTelemetry is vendor-neutral. The same instrumentation works whether you export to Jaeger, Datadog, or Azure Monitor. Switching backends is a configuration change.</p><p><strong>Alternative:</strong> Application Insights SDK or Datadog APM. Both provide richer out-of-the-box dashboards but create vendor lock-in.</p>" },
          { title: "W3C TraceContext through messaging", content: "<p><strong>Why:</strong> Without trace propagation through async messaging, traces break at the publisher. You see the API call but not the downstream processing. W3C TraceContext is the standard.</p><p><strong>Alternative:</strong> Custom correlation IDs in message payloads. Simpler but doesn't integrate with the standard tracing ecosystem.</p>" },
          { title: "Business-named spans over framework defaults", content: "<p><strong>Why:</strong> Default ASP.NET Core spans are named after HTTP methods and routes. &quot;CreateCompanyCommand&quot; tells you more than &quot;POST /api/companies&quot;.</p><p><strong>Alternative:</strong> Rely on framework-generated spans only. Less work upfront but harder to debug.</p>" },
          { title: "Separated health checks by concern", content: "<p><strong>Why:</strong> A service that can't reach its database shouldn't receive traffic, but it's still alive (don't restart it). Separating liveness from readiness prevents unnecessary restarts.</p><p><strong>Alternative:</strong> Single health endpoint that checks everything. Simpler but conflates restart with traffic routing.</p>" },
        ]} />
      </CsSection>

      <CsSection id="tradeoffs">
        <h2>Tradeoffs &amp; Lessons Learned</h2>
        <CsTradeoffList items={[
          "<strong>Instrumentation discipline:</strong> Every new service, every new handler, every new integration needs spans and structured logging. It's a habit that has to be enforced from day one.",
          "<strong>Trace storage costs at scale:</strong> Jaeger's in-memory storage works for a portfolio but wouldn't survive a restart in production. A real deployment would use Elasticsearch or Cassandra.",
          "<strong>Span overhead:</strong> Each span adds 2-5ms of overhead. For hot paths with many nested spans, it's measurable. Production systems typically use sampling to reduce this.",
          "<strong>Dashboard maintenance:</strong> Grafana dashboards drift unless someone owns them. Treating dashboards as code (provisioned from JSON) helps but doesn't eliminate the problem.",
        ]} />
      </CsSection>
    </CaseStudyLayout>
  );
}
