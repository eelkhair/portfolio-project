import type { Metadata } from "next";
import Link from "next/link";
import { Header } from "../components/Header";
import { Footer } from "../components/Footer";
import { ScreenshotSwiper } from "../components/ScreenshotSwiper";
import { DeepDivesSection, DeepDivesDropdown } from "../components/DeepDivesSection";
import { ContactNavLink } from "../components/ContactGate";
import { archHighlights, techStack, exploreCards } from "../data/portfolio-data";
import { ServiceStatusGrid } from "../components/ServiceStatusGrid";

// Required by @cloudflare/next-on-pages: every SSR route must opt into edge runtime.
export const runtime = "edge";

export const metadata: Metadata = {
  title: "Portfolio - JobBoard Distributed Systems Platform",
};

const portfolioNavLinks = [
  { href: "/", label: "About Me" },
  { href: "#portfolio", label: "Project" },
  { href: "#services", label: "Live Demo" },
  { href: "#explore", label: "Explore" },
  { href: "#architecture", label: "System Design" },
  { href: "#screenshots", label: "Screenshots" },
];

export default function PortfolioPage() {
  return (
    <>
      <Header links={portfolioNavLinks} dropdownSlot={<><DeepDivesDropdown /><ContactNavLink /></>} />
      <main id="main">
        {/* Portfolio Project */}
        <section id="portfolio" className="section-top" aria-labelledby="portfolio-heading" style={{ paddingTop: "6rem" }}>
          <div className="container">
            <p className="section-title">Portfolio Project</p>
            <h2 id="portfolio-heading">JobBoard &mdash; Distributed Systems Platform</h2>
            <p className="section-text mb-4">
              A full-stack, multi-tenant platform demonstrating real-world legacy-to-cloud migration with
              enterprise architecture patterns, AI integration, and production-grade observability.
              Three architectural paths running side-by-side with 20 Architecture Decision Records documenting every trade-off.
            </p>

            <h3 className="mb-2">Architecture Highlights</h3>
            <div className="card-grid mb-5">
              {archHighlights.map((c) => (
                <div className="card" key={c.title}>
                  <div className="card-icon" role="img" aria-hidden="true">{c.icon}</div>
                  <h3>{c.title}</h3>
                  <p>{c.desc}</p>
                </div>
              ))}
            </div>

            <h3 className="mb-2">Tech Stack</h3>
            <div className="skill-tags mb-2">
              {techStack.map((t) => <span className="skill-tag" key={t}>{t}</span>)}
            </div>
          </div>
        </section>

        {/* Live Services */}
        <section id="services" className="section-alt" aria-labelledby="services-heading">
          <div className="container">
            <p className="section-title">Live Demo</p>
            <h2 id="services-heading">Running Services</h2>
            <p className="section-text mb-3">
              All services are running on a self-hosted Proxmox homelab, exposed via Cloudflare Tunnel.
            </p>
            <div className="coming-soon-banner" role="status">
              <span className="coming-soon-icon" aria-hidden="true">{"\u2728"}</span>
              <div>
                <div className="coming-soon-title">The demo is live — no signup required</div>
                <div className="coming-soon-desc">
                  Click <strong>Try Demo Instantly</strong> on the sign-in page of the{" "}
                  <a href="https://job-admin.elkhair.tech" target="_blank" rel="noopener noreferrer" className="service-register-link">Admin app</a>
                  {" "}to post jobs and explore the admin experience, or the{" "}
                  <a href="https://jobs.elkhair.tech" target="_blank" rel="noopener noreferrer" className="service-register-link">Public app</a>
                  {" "}to apply as a candidate. Throwaway account — no email needed.
                </div>
              </div>
            </div>
            <ServiceStatusGrid />
          </div>
        </section>

        <DeepDivesSection />

        {/* How to Explore */}
        <section id="explore" aria-labelledby="explore-heading">
          <p className="section-title">Getting Started</p>
          <h2 id="explore-heading">How to Explore This Project</h2>
          <p className="section-text mb-3">
            Jump into the live apps and try the key features. Each action generates real distributed traces you can inspect.
          </p>
          <div className="card-grid">
            {exploreCards.map((c) => (
              <a href={c.href} target="_blank" rel="noopener noreferrer" className="card" key={c.title}>
                <h3>{c.title}</h3>
                <p>{c.desc}</p>
              </a>
            ))}
          </div>
        </section>

        {/* Architecture Diagram */}
        <section id="architecture" aria-labelledby="architecture-heading">
          <p className="section-title">System Architecture</p>
          <h2 id="architecture-heading">How It All Connects</h2>
          <p className="section-text mb-3">
            16 services across 4 architectural layers, orchestrated with Dapr sidecars and exposed via Cloudflare Tunnel.
          </p>
          <div className="arch-diagram" role="img" aria-label="System architecture diagram showing Clients, Gateway, Monolith/Microservices, AI Service, Infrastructure, and Observability layers.">
            <div className="arch-row">
              <div className="arch-label">Clients</div>
              <div className="arch-nodes">
                <div className="arch-node">Admin App<span>Angular 20</span></div>
                <div className="arch-node">Public App<span>Angular 21 + SSR</span></div>
              </div>
            </div>
            <div className="arch-arrow" aria-hidden="true">&darr;</div>
            <div className="arch-row">
              <div className="arch-label">Gateway</div>
              <div className="arch-nodes">
                <div className="arch-node arch-accent">YARP Gateway<span>Feature Flag Routing</span></div>
              </div>
            </div>
            <div className="arch-arrow-split">
              <div className="arch-branch">
                <div className="arch-arrow-label">monolith mode &darr;</div>
                <div className="arch-row-compact">
                  <div className="arch-node">Monolith API<span>.NET 9 &middot; DDD &middot; CQRS</span></div>
                  <div className="arch-node arch-mcp">Monolith MCP<span>AI Tool Server</span></div>
                </div>
              </div>
              <div className="arch-branch">
                <div className="arch-arrow-label">micro mode &darr;</div>
                <div className="arch-row-compact">
                  <div className="arch-node">Admin API</div>
                  <div className="arch-node">Company API</div>
                  <div className="arch-node">Job API</div>
                  <div className="arch-node">User API</div>
                  <div className="arch-node arch-mcp">Admin MCP<span>AI Tool Server</span></div>
                </div>
              </div>
            </div>
            <div className="arch-row mt-2">
              <div className="arch-label">Legacy Modernization</div>
              <div className="arch-nodes">
                <div className="arch-node">Connector API<span>Monolith &rarr; Micro</span></div>
                <div className="arch-node">Reverse Connector<span>Micro &rarr; Monolith</span></div>
              </div>
            </div>
            <div className="arch-arrow" aria-hidden="true">&darr;</div>
            <div className="arch-row">
              <div className="arch-label">AI</div>
              <div className="arch-nodes">
                <div className="arch-node arch-accent">AI Service v2<span>.NET 10 &middot; LLM Chat &middot; RAG &middot; MCP Client</span></div>
              </div>
            </div>
            <div className="arch-arrow" aria-hidden="true">&darr;</div>
            <div className="arch-row">
              <div className="arch-label">Infrastructure</div>
              <div className="arch-nodes">
                <div className="arch-node arch-infra">SQL Server</div>
                <div className="arch-node arch-infra">PostgreSQL<span>pgvector</span></div>
                <div className="arch-node arch-infra">Redis</div>
                <div className="arch-node arch-infra">RabbitMQ</div>
                <div className="arch-node arch-infra">Keycloak<span>OIDC / RBAC</span></div>
              </div>
            </div>
            <div className="arch-arrow" aria-hidden="true">&darr;</div>
            <div className="arch-row">
              <div className="arch-label">Observability</div>
              <div className="arch-nodes">
                <div className="arch-node arch-obs">OpenTelemetry</div>
                <div className="arch-node arch-obs">Jaeger</div>
                <div className="arch-node arch-obs">Grafana</div>
                <div className="arch-node arch-obs">Health Checks</div>
              </div>
            </div>
          </div>
        </section>

        {/* Screenshots Carousel */}
        <section className="section-alt-flush" role="region" aria-labelledby="screenshots-heading">
          <div className="container">
            <p className="section-title">Screenshots</p>
            <h2 id="screenshots-heading">The System in Action</h2>
            <p className="section-text mb-4">
              Explore the platform&apos;s dashboards, traces, and infrastructure. Click any slide to enlarge.
            </p>
          </div>
          <ScreenshotSwiper />
        </section>
      </main>
      <Footer />
    </>
  );
}
