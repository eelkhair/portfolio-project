# ADR-007: Trace Context Propagation

## Status
Accepted

## Context
Debugging distributed systems requires correlating frontend requests with backend execution.

## Decision
- Propagate W3C Trace Context across all services
- Return TraceId in HTTP response headers
- Correlate logs, traces, and events

## Consequences
- Faster debugging
- Improved operability
- Slight increase in instrumentation complexity
