# ADR-004: Observability-First (Tracing, Logging, Metrics, and Health Checks)

- **Status:** Accepted
- **Date:** 2026-01-03

## Context

Modern systems are judged not only by features, but by:
- how quickly issues can be diagnosed
- how safely changes can be released
- how confidently systems can be operated

Portfolios often omit this, but for senior roles, operability is a core competency.

## Decision

We treat observability as a **first-class design constraint**:

- **Distributed tracing** across services and key internal operations.
- **Structured logs** with correlation identifiers.
- Consistent **ActivitySource** usage in .NET and equivalent tracing in other runtimes.
- **Health checks** separated into liveness/readiness and dependency checks.

Telemetry conventions:
- Trace/span names reflect business operations (not framework internals).
- Tags include domain identifiers (e.g., company id, job id) without leaking sensitive data.
- Errors are recorded consistently and mapped to appropriate responses.

## Consequences

### Positive
- Faster debugging and root-cause analysis.
- Stronger “production-readiness” signal for reviewers.
- Enables meaningful performance and reliability discussions.

### Negative / Tradeoffs
- Adds up-front work and ongoing discipline.
- Requires avoiding noisy or high-cardinality telemetry.

## Implementation Notes

- Standardize trace context propagation (headers, correlation ids).
- Health endpoints:
  - **/livez**: process liveness
  - **/readyz**: dependencies readiness
- Dashboards/log aggregation are optional for local dev, but hooks should exist.
