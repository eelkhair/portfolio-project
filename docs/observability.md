# Observability

This platform is instrumented **end-to-end** using **OpenTelemetry** and a tracing + logs workflow designed for real-world debugging.

The main idea:
- Every request is traceable from **browser → API → database → async pub/sub → downstream services**
- A single **TraceId** can be used to pivot between **Jaeger** and **Grafana** (logs/events view)

---

## What’s included

### Distributed tracing (OpenTelemetry)
- HTTP spans for inbound/outbound requests
- Dependency spans (DB calls)
- Pub/Sub spans (publish + consume)
- Correlated attributes/tags to make traces searchable

### Trace correlation in logs
Logs include trace context so you can:
- filter logs by TraceId
- group by service
- replay the story of a request

### “Find by TraceId” in Grafana
A dashboard that lets you paste a TraceId and immediately see:
- the request path
- the services involved
- time-ordered logs/events correlated to the trace

---

## Walkthrough: Browser → API → Jaeger → Grafana

### 1) Get the TraceId from the browser
When you trigger a request in the Admin UI, the API returns a `Trace-Id` response header.

Example screenshot:
- `images/Observability/ui-browser-trace-id-header.png`

### 2) Jump into Jaeger
Paste that TraceId into Jaeger to view the distributed trace:
- service map / timeline
- parent/child spans
- hotspots and slow dependencies

Example screenshots:
- `images/Observability/jaeger-end-to-end-trace.png`
- `images/Observability/distributed-trace-end-to-end-jaeger.png`

### 3) Pivot to logs/events in Grafana
Use the same TraceId in Grafana to view correlated logs/events across services.

Example screenshots:
- `images/Observability/grafana-find-by-trace-id.png`
- `images/Observability/logs-trace-correlation-grafana.png`

---

## Async visibility (Saga + pub/sub)

Async work is visible as part of the overall request story:
- initial request publishes an integration event
- consumers process it in downstream services
- saga steps are logged for a time-ordered narrative

Example screenshot:
- `images/Observability/async-saga-and-pubsub-visibility.png`

---

## Recommended conventions

These conventions make the observability UX excellent:

- **Expose TraceId** to the UI as a response header (`Trace-Id`), and expose it via CORS headers.
- **Use consistent service names** (e.g., `monolith-api`, `connector-api`, `company-api`, etc.).
- Add **domain tags** to spans/logs:
  - company id / company name
  - user email
  - saga id / idempotency key
- Ensure **retry-aware logging** (e.g., attempt number, message id, DLQ routing when relevant).

---

## Screenshots index

- TraceId in browser headers: `images/Observability/ui-browser-trace-id-header.png`
- Trace propagation: `images/Observability/trace-id-propagation-browser-to-backend.png`
- Jaeger end-to-end: `images/Observability/jaeger-end-to-end-trace.png`
- Distributed trace view: `images/Observability/distributed-trace-end-to-end-jaeger.png`
- Grafana main: `images/Observability/grafana.png`
- Grafana “find by TraceId”: `images/Observability/grafana-find-by-trace-id.png`
- Trace ↔ logs correlation: `images/Observability/logs-trace-correlation-grafana.png`
- Async saga visibility: `images/Observability/async-saga-and-pubsub-visibility.png`
