## Screenshots (Curated Evidence)

This repository includes **curated screenshots** to make the architecture and operational story easy to validate **without reading every line of code**.

> **Interview tip:** Walk through a single business flow (e.g. *Create Company → Publish Event → Downstream Processing*) and use these screenshots as proof points.

---

## Observability

### TraceId exposed in the browser

Each request exposes a `Trace-Id` response header, making trace correlation immediately visible to frontend developers and operators.

![TraceId in browser response headers](Images/Observability/ui-browser-trace-id-header.png)

---

### Trace propagation (browser → backend)

The same TraceId is propagated consistently across services and async boundaries.

![Trace propagation browser to backend](Images/Observability/trace-id-propagation-browser-to-backend.png)

---

### End-to-end distributed tracing (Jaeger)

A single TraceId reveals the full request path across services, dependencies, and async workflows.

![Jaeger end-to-end trace](Images/Observability/jaeger-end-to-end-trace.png)

![Distributed trace timeline](Images/Observability/distributed-trace-end-to-end-jaeger.png)

---

### Logs and events correlated by TraceId (Grafana)

Traces pivot cleanly into logs and events, enabling fast root-cause analysis.

![Grafana find by TraceId](Images/Observability/grafana-find-by-trace-id.png)

![Trace and logs correlation](Images/Observability/logs-trace-correlation-grafana.png)

---

### Async saga and pub/sub visibility

Asynchronous workflows (publish/consume + saga steps) appear as part of the same request narrative.

![Async saga and pub/sub visibility](Images/Observability/async-saga-and-pubsub-visibility.png)

---

## Strangler Fig Migration

These screenshots illustrate the **incremental migration strategy** from monolith to services.

### Stage 1 — Monolith primary

The monolith handles all requests and publishes integration events.

![Strangler Fig stage 1 – monolith primary](Images/Strangler%20Fig/strangler-fig-stage-1-monolith-primary.png)

---

### Stage 2 — Connector introduced

A connector layer routes selected flows while preserving backward compatibility.

![Strangler Fig stage 2 – connector introduced](Images/Strangler%20Fig/strangler-fig-stage-2-connector-introduced.png)

---

### Stage 3 — Service extraction

Capabilities are extracted into independent services without breaking consumers.

![Strangler Fig stage 3 – service extraction](Images/Strangler%20Fig/strangler-fig-stage-3-service-extraction.png)

---

### Stage 4 — Deprecated paths

Legacy paths are deprecated and removed once traffic is fully migrated.

![Strangler Fig stage 4 – deprecated paths](Images/Strangler%20Fig/strangler-fig-stage-4-monolith-deprecated-paths.png)

---

### Centralized logs and saga sequencing

End-to-end visibility across services during the migration.

![Centralized logs (Elastic)](Images/Strangler%20Fig/strangler-fig-centralized-logs-elastic.png)

![Saga log sequence](Images/Strangler%20Fig/strangler-fig-saga-log-sequence.png)

---

## Health checks and runtime tooling

These screenshots are captured from a **running environment**, not mocks.

### Central health dashboard

Aggregated health across APIs and critical dependencies.

![Health overview – all services](Images/Healthchecks/healthchecks-overview-all-services.png)

![Monolith dependency health](Images/Healthchecks/healthchecks-monolith-dependencies.png)

---

### Dapr runtime dashboard

Runtime visibility into applications, components, and pub/sub subscriptions.

![Dapr application runtime summary](Images/healthchecks/dapr-application-runtime-summary.png)

![Dapr components status](Images/healthchecks/dapr-components-status.png)

![Dapr pub/sub subscriptions](Images/healthchecks/dapr-pubsub-subscriptions.png)

---

### Messaging and configuration infrastructure

Operational views of messaging and configuration state used for runtime diagnostics and troubleshooting.

These dashboards make it easy to answer questions like:
- Are messages backing up?
- Are retries or DLQs involved?
- Which feature flags or config values are currently active?

![RabbitMQ queues and DLQs](Images/healthchecks/rabbitmq-queues-and-dlq.png)

![Redis config and feature flags](Images/healthchecks/redis-config-and-feature-flags.png)

> Together with tracing and health checks, these views allow fast differentiation
> between application bugs, infrastructure issues, and configuration mistakes.

