# Screenshots (Curated Evidence)

This repo includes screenshots to make the architecture and operational story **easy to validate** without reading every line of code.

> Tip: In interviews, walk people through **one business flow** and use the screenshots as proof points.

---

## Observability

- TraceId in browser headers: `images/Observability/ui-browser-trace-id-header.png`
- Trace propagation browser → backend: `images/Observability/trace-id-propagation-browser-to-backend.png`
- Jaeger end-to-end trace: `images/Observability/jaeger-end-to-end-trace.png`
- Distributed trace view: `images/Observability/distributed-trace-end-to-end-jaeger.png`
- Grafana dashboards: `images/Observability/grafana.png`
- Grafana “Find by TraceId”: `images/Observability/grafana-find-by-trace-id.png`
- Trace ↔ logs correlation: `images/Observability/logs-trace-correlation-grafana.png`
- Async saga + pub/sub visibility: `images/Observability/async-saga-and-pubsub-visibility.png`

---

## Strangler Fig

- Stage 1 (monolith primary): `images/Strangler Fig/strangler-fig-stage-1-monolith-primary.png`
- Stage 2 (connector introduced): `images/Strangler Fig/strangler-fig-stage-2-connector-introduced.png`
- Stage 3 (service extraction): `images/Strangler Fig/strangler-fig-stage-3-service-extraction.png`
- Stage 4 (deprecated paths): `images/Strangler Fig/strangler-fig-stage-4-monolith-deprecated-paths.png`
- Centralized logs: `images/Strangler Fig/strangler-fig-centralized-logs-elastic.png`
- Saga sequence: `images/Strangler Fig/strangler-fig-saga-log-sequence.png`

---

## Health checks and runtime tooling

These are screenshots captured from your running environment (health dashboard, dapr dashboard, rabbitmq, redis commander):

- Health dashboard: `images/healthchecks/Snag_63566621.png`, `images/healthchecks/Snag_6356665f.png`
- Dapr dashboard (components/subscriptions/summary/apps):  
  `images/healthchecks/Snag_6356667e.png`, `images/healthchecks/Snag_6356669e.png`, `images/healthchecks/Snag_635666ec.png`, `images/healthchecks/Snag_6356670b.png`
- RabbitMQ queues + DLQs: `images/healthchecks/Snag_6356672a.png`
- Config/feature flags in Redis: `images/healthchecks/Snag_635666cc.png`

If your repo stores these under a different folder, update the paths accordingly.
