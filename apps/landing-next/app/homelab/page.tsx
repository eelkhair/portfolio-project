import type { Metadata } from "next";
import { CaseStudyLayout, CsSection, CsDecisionGrid, CsTradeoffList, CsScreenshot } from "../components/CaseStudyLayout";

// Required by @cloudflare/next-on-pages: every SSR route must opt into edge runtime.
export const runtime = "edge";

export const metadata: Metadata = {
  title: "Homelab Infrastructure",
  description: "Running a distributed platform on a self-hosted Proxmox homelab — Cloudflare Tunnel, private Docker registry, .NET Aspire orchestration, and dual-zone DNS.",
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

export default function HomelabPage() {
  return (
    <CaseStudyLayout
      title="Homelab Infrastructure"
      summary="The live demo runs on a self-hosted Proxmox homelab, exposed through Cloudflare Tunnel &mdash; with a private registry, declarative edge config, and a single-command local environment that mirrors production."
      toc={toc}
      prevLink={{ href: "/observability", label: "Previous: Observability" }}
      nextLink={{ href: "/adrs", label: "Next: ADRs" }}
    >
      <CsSection id="problem">
        <h2>The Problem</h2>
        <p>Most portfolio projects deploy to one cloud and call it done. That tells a recruiter the candidate can click &quot;deploy&quot; in a managed UI. It doesn&apos;t show that they understand what happens underneath.</p>
        <p>I wanted the live demo to run on infrastructure I actually owned &mdash; Proxmox servers in my home office &mdash; while still demonstrating production patterns: zero-trust edge networking, a private container registry, infrastructure-as-code, and a local dev environment that a new contributor can boot with one command. No &quot;works on my machine&quot; surprises between local and prod.</p>
      </CsSection>

      <CsSection id="solution" alt>
        <h2>The Solution</h2>
        <p>Five Proxmox VMs, each with a single responsibility: two application hosts (dev + prod), one infrastructure VM (reverse proxy, Cloudflare tunnel daemon, private Docker registry, WireGuard, Portainer, Uptime Kuma), one observability VM (OpenTelemetry Collector, Jaeger, Grafana, Alloy), and one DNS VM (Pi-hole).</p>
        <p>Public traffic enters through a <strong>Cloudflare Tunnel</strong> &mdash; no open inbound ports on the home network, no dynamic DNS, no VPN for end users. WAF rules, DNS records, and ingress routes are all <strong>declarative</strong>: a single GitHub Actions workflow reads <code>deploy/cloudflare/tunnel-config.json</code> and syncs Cloudflare to match.</p>
        <p>Locally, <strong>.NET Aspire</strong> orchestrates the entire platform &mdash; 36 resources including .NET services, Dapr sidecars, SQL Server, PostgreSQL, Redis, RabbitMQ, Keycloak, Elasticsearch, Jaeger, Grafana, and both Angular apps &mdash; with one command and dependency-aware startup ordering.</p>
        <CsScreenshot src="/images/aspire-graph.png" alt="Aspire topology graph showing service dependencies across the full platform" caption="Aspire topology graph — 36 resources with dependency edges, health status, and live logs in one dashboard" />
      </CsSection>

      <CsSection id="architecture">
        <h2>Architecture</h2>
        <p>The homelab runs on a single Proxmox cluster with purpose-built VMs. Each VM has a static LAN IP and one job:</p>
        <ul className="cs-tradeoff-list">
          <li><strong>192.168.1.200</strong> &mdash; dev host. Runs the dev Docker Compose stack (gateway, monolith, microservices, AI service, two Angular apps, Dapr sidecars).</li>
          <li><strong>192.168.1.112</strong> &mdash; prod host. Same stack, prod configuration.</li>
          <li><strong>192.168.1.150</strong> &mdash; infrastructure. Nginx Proxy Manager (LAN TLS termination), Cloudflare tunnel daemon, self-hosted Docker registry at <code>registry.eelkhair.net</code>, WireGuard, Portainer, Uptime Kuma.</li>
          <li><strong>192.168.1.115</strong> &mdash; HashiCorp Vault (<code>:8200</code>) + Grafana (<code>:3000</code>, exposed publicly at <code>grafana.elkhair.tech</code>). Vault is the single source of truth for secrets (DB credentials, Keycloak client secrets, API keys, OAuth2 client credentials). Every Dapr sidecar mounts a <code>secretstores.hashicorp.vault</code> component pointed here.</li>
          <li><strong>192.168.1.160</strong> &mdash; observability. OpenTelemetry Collector (<code>:4318</code> HTTP / <code>:4317</code> gRPC, exposed publicly at <code>otel.elkhair.tech</code>), Jaeger (<code>:16686</code>, exposed at <code>jaeger.elkhair.tech</code>), Prometheus, and Grafana Alloy (Faro browser-telemetry receiver).</li>
          <li><strong>192.168.1.123</strong> &mdash; Pi-hole. Local DNS overrides for internal-only subdomains.</li>
        </ul>
        <p>Two public domains point at the same infrastructure: <code>eelkhair.net</code> (the original) and <code>elkhair.tech</code> (the portfolio-branded one). Both resolve through the same Cloudflare Tunnel, both carry valid certificates at the edge, and CORS is allowlisted across zones so Angular apps on one domain can call APIs on the other. This lets me evolve branding without rebuilding infrastructure.</p>
        <CsScreenshot src="/images/aspire-resources.png" alt="Aspire resources view showing all 36 services, containers, and their health status" caption="Aspire resources dashboard — every service, sidecar, and container visible with logs, environment, endpoints, and dependency state" />
      </CsSection>

      <CsSection id="user-view" alt>
        <h2>What You See</h2>
        <p>As a visitor, you don&apos;t see any of this. You type <code>jobs.elkhair.tech</code>, TLS terminates at Cloudflare&apos;s edge, traffic flows through the tunnel to the reverse proxy on <code>192.168.1.150</code>, which routes to the gateway container, which routes to either the monolith or microservices based on the <code>x-mode</code> header. The experience feels identical to a cloud deployment.</p>
        <p>As a developer cloning the repo, you run one command: <code>dotnet run --project aspire/JobBoard.AppHost</code>. Aspire pulls down container images, seeds databases from backups (<code>.bak</code> and <code>.dump</code> files via a single &quot;seed-runner&quot; container that runs SQL Server, Postgres, and Redis seeds in parallel), waits for health endpoints, then launches .NET projects as native processes (debugger-attachable) with Dapr sidecars wired up. 36 resources, dependency-ordered, in a single dashboard.</p>
        <p>As a recruiter, you don&apos;t have to take my word for any of this. <strong>Jaeger is public</strong> at <a href="https://jaeger.elkhair.tech" target="_blank" rel="noopener noreferrer">jaeger.elkhair.tech</a> &mdash; open it after you click around the live apps and you&apos;ll see your own requests as distributed traces, spanning gateway &rarr; monolith or microservices &rarr; Dapr pub/sub &rarr; AI service. <strong>Grafana is public</strong> at <a href="https://grafana.elkhair.tech" target="_blank" rel="noopener noreferrer">grafana.elkhair.tech</a> (anonymous viewer role) with dashboards for Web RUM, Monolith Overview, AI Service, and &quot;Find by Trace ID&quot; for end-to-end log/trace correlation.</p>
        <div className="cs-evidence-grid">
          <CsScreenshot src="/images/healthchecks.png" alt="Health checks dashboard showing every service, dependency, and sidecar" caption="Centralized health check dashboard — every service, Dapr component, pub/sub topic, and external dependency monitored from a single page" />
          <CsScreenshot src="/images/dapr-dashboard.png" alt="Dapr dashboard showing all applications, components, and sidecar topology" caption="Dapr dashboard — sidecars, shared components, and per-service configuration surfaced for every running app" />
        </div>
      </CsSection>

      <CsSection id="behind-scenes">
        <h2>Behind the Scenes</h2>
        <p><strong>Declarative Cloudflare.</strong> The tunnel&apos;s ingress rules, public hostnames, DNS records across both zones, WAF rules, and zone-level security settings are all committed at <code>deploy/cloudflare/tunnel-config.json</code>. A GitHub Actions workflow (<code>cloudflare-tunnel.yml</code>) reads that file and uses the Cloudflare API to reconcile edge state on every merge. Adding a new subdomain is a pull request, not a dashboard click.</p>
        <p><strong>Private registry with garbage collection.</strong> The self-hosted registry at <code>registry.eelkhair.net</code> runs with <code>REGISTRY_STORAGE_DELETE_ENABLED=true</code>, and a weekly cron runs <code>registry garbage-collect --delete-untagged</code> to reclaim untagged blob space. Without this, every CI build leaks blob storage forever &mdash; a common self-hosted-registry foot-gun.</p>
        <p><strong>Seed runner pattern.</strong> Aspire&apos;s <code>WaitFor()</code> only works if the thing being waited on exposes a health endpoint. The seed runner is a single persistent container that runs Redis, SQL Server, and Postgres seeding scripts in parallel on startup, then opens an HTTP health endpoint on port 8080 and idles. Every service uses <code>.WaitFor(seedRunner)</code>, which blocks until all three databases are seeded. This eliminated a cold-start race condition where services started before their databases were ready.</p>
        <p><strong>Local DNS caveat.</strong> Pi-hole overrides <code>*.eelkhair.net</code> to <code>192.168.1.150</code> so internal LAN traffic skips the public internet path. A wildcard Let&apos;s Encrypt cert on the Nginx Proxy Manager makes that work. The same trick does <em>not</em> work for <code>*.elkhair.tech</code> &mdash; no wildcard cert &mdash; so <code>elkhair.tech</code> always resolves publicly through Cloudflare, even from inside the house. A one-line rule difference; discovered the hard way.</p>
        <p><strong>Secrets via HashiCorp Vault.</strong> Every service reads its secrets (connection strings, Keycloak client secrets, API keys, OAuth2 client credentials for service-to-service calls) from a self-hosted Vault instance at <code>192.168.1.115:8200</code>, accessed through Dapr&apos;s <code>secretstores.hashicorp.vault</code> component. Each service has a scoped <code>secret.yaml</code> in its Dapr components directory; rotation is a Vault write plus a sidecar restart, never a code change or a redeploy. No secrets live in compose files, no secrets live in Git. The same config surface exists in Azure under <code>secretstores.azure.keyvault</code> &mdash; the service code doesn&apos;t know the difference.</p>
        <p><strong>Cold-standby landing page.</strong> The landing site is also deployed to Cloudflare Pages at <code>landing-backup.elkhair.tech</code> on every merge. When the homelab goes dark &mdash; ISP outage, power cut, hardware failure &mdash; a single command (<code>deploy/cloudflare/failover-landing.sh cf [dev|prod|both]</code>) upserts the apex and <code>www</code> CNAMEs to point at the Pages deployment instead of the tunnel. Running it with <code>proxmox</code> as the first argument flips back. The script is idempotent and scoped (dev, prod, or both), so re-runs are safe and partial failover is one flag away. DNS inside Cloudflare propagates near-instantly; the 300-second TTL caps external cache. It&apos;s manual by design &mdash; an outage here is never a surprise and doesn&apos;t warrant the cost of Cloudflare Load Balancing.</p>
      </CsSection>

      <CsSection id="decisions" alt>
        <h2>Key Decisions</h2>
        <CsDecisionGrid decisions={[
          { title: "Proxmox homelab over single cloud provider", content: "<p><strong>Why:</strong> Full control over the stack, zero ongoing cloud spend, and a realistic story about understanding infrastructure end-to-end &mdash; not just the managed control plane. Bicep IaC is still committed for Azure as a reference deployment path.</p><p><strong>Alternative:</strong> Azure Container Apps only. Simpler to operate but tells a less differentiated story for a portfolio, and the monthly bill would be meaningful for a project with no revenue.</p>" },
          { title: "Cloudflare Tunnel over self-managed VPN + DDNS", content: "<p><strong>Why:</strong> Zero open inbound ports on the home network. No dynamic DNS, no port-forwarding, no NAT traversal. TLS terminates at Cloudflare's edge with Universal SSL. WAF and DDoS mitigation are free.</p><p><strong>Alternative:</strong> WireGuard + DuckDNS. Works but exposes the home network surface and requires managing certificates and renewals manually.</p>" },
          { title: ".NET Aspire over Docker Compose for local dev", content: "<p><strong>Why:</strong> Aspire orchestrates .NET projects as <em>native processes</em> (debugger-attachable) while still managing container infrastructure and Dapr sidecars. Compose forces you to containerize everything — slow rebuild cycles, no debugger attach. Compose is still used for dev and prod deployments, so the production story stays unchanged.</p><p><strong>Alternative:</strong> Docker Compose for everything or Microsoft's retired <code>tye</code> tool. Aspire won on debugger ergonomics and active maintenance.</p>" },
          { title: "Declarative Cloudflare config over clicking the dashboard", content: "<p><strong>Why:</strong> Ingress rules, WAF rules, and DNS records are version-controlled, reviewable in PRs, and reproducible. Losing the Cloudflare dashboard is a zero-impact event — re-running the workflow restores everything.</p><p><strong>Alternative:</strong> Manual dashboard configuration. Faster initially but drift is invisible and disaster recovery is guesswork.</p>" },
        ]} />
      </CsSection>

      <CsSection id="tradeoffs">
        <h2>Tradeoffs &amp; Lessons Learned</h2>
        <CsTradeoffList items={[
          "<strong>Home power and ISP are the SLA &mdash; with a manual escape hatch.</strong> A power outage or ISP blip would take the <em>dynamic</em> parts of the demo down (live apps, APIs), but the landing page itself survives: Cloudflare Pages hosts a cold standby at <code>landing-backup.elkhair.tech</code>, and the failover script flips the apex CNAME to it in one command. Good enough for a portfolio; not a substitute for real HA.",
          "<strong>Single-host ceiling per environment.</strong> Docker Compose has no rolling updates, no horizontal auto-scaling, no self-healing orchestration. The architecture is designed to support all of it (stateless services, external state stores, health-gated readiness), but proving it would require migrating to Kubernetes or Container Apps.",
          "<strong>Registry disk fills silently.</strong> Before the weekly GC cron landed, image pushes started returning HTTP 500 mid-blob-upload as <code>/mnt/storage</code> filled. The storage path looks like a network mount but is actually a local disk. Monitoring disk on the infrastructure VM is now the first thing I check when pushes fail.",
          "<strong>Three configuration sources.</strong> Between Dapr vault, Redis config store, and environment variables, adding a new service means touching three places. Aspire local papers over this; production deployments don&apos;t. Worth it for the flexibility, but the surface area is real.",
          "<strong>Cloudflare is a single vendor dependency &mdash; but the blast radius is bounded.</strong> Both the tunnel and the backup Pages deployment live in the same Cloudflare account, so a full-account issue would still take everything down. The failover is a <em>homelab</em> fallback, not a <em>Cloudflare</em> fallback. Acceptable for a portfolio; a real product would want a second edge provider.",
        ]} />
      </CsSection>
    </CaseStudyLayout>
  );
}
