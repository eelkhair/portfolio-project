import type { Metadata } from "next";
import { CaseStudyLayout, CsSection, CsDecisionGrid, CsTradeoffList, CsScreenshot } from "../components/CaseStudyLayout";

// Required by @cloudflare/next-on-pages: every SSR route must opt into edge runtime.
export const runtime = "edge";

export const metadata: Metadata = {
  title: "Architecture Overview",
  description: "How a monolith, microservices, and a strangler-fig migration run side-by-side in one codebase with YARP gateway routing.",
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

export default function ArchitecturePage() {
  return (
    <CaseStudyLayout
      title="Architecture Overview"
      summary="How a monolith, microservices, and a strangler-fig migration run side-by-side in one codebase &mdash; toggled per session, routed by a single gateway."
      toc={toc}
      prevLink={{ href: "/portfolio", label: "Back to Portfolio" }}
      nextLink={{ href: "/ai-orchestration", label: "Next: AI Orchestration" }}
    >
      <CsSection id="problem">
        <h2>The Problem</h2>
        <p>Most portfolio projects show one architecture style. But real systems evolve. Monoliths get decomposed. Services get extracted. Data has to stay in sync during the transition.</p>
        <p>The question I wanted to answer: how do you demonstrate a monolith-to-microservices migration without a big-bang rewrite, and let someone switch between both paths to see the difference?</p>
      </CsSection>

      <CsSection id="solution" alt>
        <h2>The Solution</h2>
        <p>Three architectural paths running simultaneously in the same codebase:</p>
        <p>A <strong>clean architecture monolith</strong> with DDD, CQRS, and a custom decorator pipeline. A set of <strong>decomposed microservices</strong> built with FastEndpoints across four bounded contexts. And a <strong>connector API</strong> implementing the strangler-fig pattern with bidirectional sync between both sides.</p>
        <p>A YARP reverse proxy sits in front of everything. It reads an <code>x-mode</code> header on every request and routes to the monolith or microservices accordingly. Each user session picks its own path &mdash; no backend state change, no restart.</p>
        <CsScreenshot src="/images/admin-toolbar-toggle.png" alt="Admin toolbar with monolith/micro toggle button" caption="The admin toolbar lets visitors toggle between monolith and microservices mode per session" />
      </CsSection>

      <CsSection id="architecture">
        <h2>Architecture</h2>
        <p>The gateway is the single entry point. It inspects the <code>x-mode</code> header &mdash; set by the frontend based on a localStorage toggle &mdash; and proxies to either the monolith API or the microservices cluster.</p>
        <p>Both paths share SQL Server as their primary database and the same messaging infrastructure (RabbitMQ via Dapr pub/sub). PostgreSQL is used exclusively by the AI service for pgvector embeddings. A <strong>connector API</strong> and <strong>reverse connector API</strong> handle bidirectional sync: when a company is created in the monolith, the connector saga fans out to provision it across all four microservices. When created in microservices, the reverse connector syncs it back to the monolith.</p>
        <CsScreenshot src="/images/gateway-routing-trace.png" alt="Jaeger traces comparing gateway routing to microservices (top) vs monolith (bottom)" caption="Jaeger traces side-by-side: microservices path fans out to admin-api and company-api (top), monolith path routes directly to monolith-api (bottom)" />
      </CsSection>

      <CsSection id="user-view" alt>
        <h2>What You See</h2>
        <p>From the admin app, click the mode toggle in the toolbar. The UI stays the same &mdash; same forms, same pages, same data. But behind the scenes, the entire backend path changes.</p>
        <p>Create a company in monolith mode, then switch to microservices mode. The company is there &mdash; the connector saga synced it. Open Jaeger and compare the two traces: one shows a single monolith span, the other shows the request fan out across four services.</p>
        <div className="cs-evidence-grid">
          <CsScreenshot src="/images/saga-trace.png" alt="Jaeger trace showing saga connector fanning out across services" caption="Saga orchestration fanning out a company creation across admin, company, job, and user APIs" />
          <CsScreenshot src="/images/dapr-dashboard.png" alt="Dapr dashboard showing sidecar topology and component bindings" caption="Dapr dashboard showing sidecar topology and component bindings across all services" />
        </div>
      </CsSection>

      <CsSection id="behind-scenes">
        <h2>Behind the Scenes</h2>
        <p>The monolith uses a <strong>transactional outbox</strong> to publish domain events reliably. Events are written to an outbox table in the same transaction as the domain change, then dispatched to RabbitMQ by a background processor. This guarantees at-least-once delivery without distributed transactions.</p>
        <p>The connector API implements an <strong>orchestrated saga</strong> for cross-service provisioning. When a company is created in the monolith, the saga calls each microservice in sequence, tracks completion state, and handles compensating actions on failure. Redis idempotency keys prevent duplicate processing.</p>
        <p>The reverse connector handles the opposite direction: microservice events flow back to the monolith. This bidirectional sync means both paths always have the same data, which is the key requirement for a strangler-fig migration &mdash; you can switch traffic gradually without data loss.</p>
      </CsSection>

      <CsSection id="decisions" alt>
        <h2>Key Decisions</h2>
        <CsDecisionGrid decisions={[
          { title: "YARP direct proxy over Dapr service invocation", content: "<p><strong>Why:</strong> YARP gives full control over routing rules, header manipulation, and load balancing. Dapr invocation adds indirection that complicates debugging.</p><p><strong>Alternative:</strong> Dapr service invocation with custom middleware. Rejected because it would require reimplementing routing logic that YARP handles out of the box.</p>" },
          { title: "Transactional outbox over direct event publishing", content: "<p><strong>Why:</strong> Direct publishing to RabbitMQ risks message loss if the process crashes after the database commit but before the publish completes. The outbox guarantees atomicity.</p><p><strong>Alternative:</strong> Change Data Capture (CDC) from the database transaction log. More complex to operate.</p>" },
          { title: "Orchestrated saga over choreography", content: "<p><strong>Why:</strong> With four microservices involved in provisioning, choreography creates implicit coupling and makes failure handling opaque. An orchestrator makes the flow explicit and debuggable in traces.</p><p><strong>Alternative:</strong> Event choreography. Simpler initially but harder to reason about as the number of services grows.</p>" },
          { title: "Docker Compose over Kubernetes", content: "<p><strong>Why:</strong> For a portfolio running on a Proxmox homelab, Kubernetes adds operational overhead without proportional benefit. Docker Compose with Dapr sidecars provides service mesh capabilities at a fraction of the complexity.</p><p><strong>Alternative:</strong> K3s or MicroK8s. Would demonstrate Kubernetes experience but at the cost of maintaining a cluster.</p>" },
        ]} />
      </CsSection>

      <CsSection id="tradeoffs">
        <h2>Tradeoffs &amp; Lessons Learned</h2>
        <CsTradeoffList items={[
          "<strong>Repo complexity vs. breadth:</strong> Maintaining three architectural paths in one repo is unusual. It works here because it's a portfolio project with one developer. In a team setting, you'd pick one path and migrate incrementally.",
          "<strong>Eventual consistency window:</strong> The saga takes seconds to propagate changes across services. During that window, the microservices path may not reflect a change made via the monolith.",
          "<strong>No auto-scaling:</strong> Docker Compose doesn't support horizontal scaling. The architecture is designed for it (stateless services, external state stores), but proving it would require Kubernetes or Azure Container Apps.",
          "<strong>Operational overhead:</strong> 16 containers plus Dapr sidecars is a lot to monitor. The investment in observability pays for itself &mdash; without it, debugging cross-service issues would be guesswork.",
        ]} />
      </CsSection>
    </CaseStudyLayout>
  );
}
