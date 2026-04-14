"use client";

import Link from "next/link";
import { useFeatureFlags } from "./FeatureFlags";

export function DeepDivesSection() {
  const { deepDives } = useFeatureFlags();
  if (!deepDives) return null;

  return (
    <section id="deep-dives" aria-labelledby="deep-dives-heading">
      <p className="section-title">Deep Dives</p>
      <h2 id="deep-dives-heading">Case Studies</h2>
      <p className="section-text mb-3">
        Each case study covers a specific engineering topic: the problem, the solution, architecture decisions, and tradeoffs.
      </p>
      <div className="card-grid">
        <Link href="/architecture" className="card">
          <div className="card-icon" role="img" aria-hidden="true">{"\u{1F3D7}\uFE0F"}</div>
          <h3>Architecture Overview</h3>
          <p>Monolith vs microservices, YARP gateway routing, Dapr sidecars, and the strangler-fig migration strategy with bidirectional sync.</p>
        </Link>
        <Link href="/ai-orchestration" className="card">
          <div className="card-icon" role="img" aria-hidden="true">{"\u{1F9E0}"}</div>
          <h3>AI Orchestration</h3>
          <p>Multi-provider LLM chat, MCP server tool discovery, scoped tool registries, and an event-driven resume RAG pipeline with pgvector.</p>
        </Link>
        <Link href="/observability" className="card">
          <div className="card-icon" role="img" aria-hidden="true">{"\u{1F50D}"}</div>
          <h3>Observability</h3>
          <p>Distributed tracing with Jaeger, Grafana dashboards, Seq structured logging, and OpenTelemetry instrumentation across 16 services.</p>
        </Link>
      </div>
    </section>
  );
}

export function DeepDivesDropdown() {
  const { deepDives } = useFeatureFlags();
  if (!deepDives) return null;

  return (
    <li className="nav-dropdown">
      <a href="#deep-dives" aria-haspopup="true">Deep Dives</a>
      <ul className="nav-dropdown-menu">
        <li><Link href="/architecture">Architecture</Link></li>
        <li><Link href="/ai-orchestration">AI Orchestration</Link></li>
        <li><Link href="/observability">Observability</Link></li>
      </ul>
    </li>
  );
}
