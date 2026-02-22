# ADR-008: YARP Gateway with Direct HTTP Proxying (Bypass Dapr Service Invocation)

- **Status:** Accepted
- **Date:** 2026-02-22

## Context

The platform introduced a YARP-based API gateway to unify frontend access behind a single URL. The gateway routes requests to either the monolith or admin API based on a Dapr feature flag, and forwards AI requests to ai-service-v2 with a path prefix strip.

The initial implementation proxied all traffic through **Dapr service invocation** (`http://localhost:3500/v1.0/invoke/{appId}/method/`). This worked in local development with `dapr run`, but failed in Docker Compose deployment:

1. **Dapr's `oauth2clientcredentials` HTTP middleware** replaced the user's JWT with a machine-to-machine token, causing 401s on all proxied requests.
2. After removing that middleware, Dapr's **HTTP-to-gRPC-to-HTTP conversion** during sidecar-to-sidecar communication still corrupted the `Authorization` header, producing `invalid_token` errors.
3. YARP's `RequestHeadersCopy` transform and explicit `AddRequestTransform` both correctly forwarded headers to the Dapr sidecar, but the header was lost or modified within Dapr's internal gRPC transport.

The root cause: Dapr service invocation is designed for **service-to-service RPC**, not transparent HTTP reverse proxying where headers must pass through unmodified.

## Decision

The YARP gateway proxies **directly** to backend services via Docker DNS, bypassing Dapr service invocation entirely for the proxy layer.

```csharp
// Before: Dapr service invocation
Address = "http://localhost:3500/v1.0/invoke/monolith-api/method/"

// After: Direct HTTP via Docker DNS
Address = "http://monolith-api:8080/"
```

The gateway's Dapr sidecar is retained for:
- Configuration management (feature flags via Redis)
- Secret access (HashiCorp Vault)
- Health checks

The gateway's Dapr config no longer includes `httpPipeline` middleware.

### Route Normalization

To enable transparent proxying, backend routes were aligned so the gateway can route to either service without path rewriting:

| Operation | Aligned Route | Notes |
|-----------|--------------|-------|
| List companies | `GET /companies` | Dual route on monolith OData controller |
| List industries | `GET /industries` | Dual route added |
| List users | `GET /users` | Dual route added |
| Generate draft | `POST /jobs/{id}/generate` | Moved from DraftsController to JobsController |
| List drafts | `GET /jobs/{id}/list-drafts` | Already aligned |
| Rewrite draft | `PUT /jobs/drafts/rewrite` | Already aligned |
| AI chat | `POST /ai/v2/chat` | PathRemovePrefix strips `/ai/v2` |

## Rationale

- **Transparent header passthrough**: Direct HTTP preserves `Authorization`, `Content-Type`, and all other headers exactly as sent by the client. No gRPC metadata conversion.
- **Simpler debugging**: One fewer hop in the trace (no Dapr sidecar intermediary for proxied traffic).
- **Dapr is the wrong tool for reverse proxying**: Dapr service invocation adds mTLS, retries, and tracing to inter-service calls. A gateway proxy needs passthrough semantics, not RPC semantics.
- **YARP provides sufficient proxy features**: Load balancing, health checks, retries, and transforms are handled natively by YARP without Dapr.

## Consequences

### Positive
- User JWTs pass through to backend services unmodified
- Eliminates an entire class of header-forwarding bugs
- Faster proxying (one fewer network hop)
- Gateway Dapr config is simpler (no HTTP pipeline middleware)

### Tradeoffs
- Gateway must be on the same Docker network as backend services (already the case)
- Service addresses are resolved via Docker DNS rather than Dapr's mDNS name resolution
- If a service name changes in Docker Compose, the YARP cluster config must be updated

### Not Affected
- Dapr pub/sub, configuration, and secrets continue to work via the gateway's sidecar
- Distributed tracing still propagates via W3C `traceparent` headers (OpenTelemetry)
- Other services continue using Dapr service invocation for inter-service RPC where header passthrough is not required

## Lessons Learned

1. **Dapr service invocation is not a transparent proxy.** Its HTTP-to-gRPC-to-HTTP conversion can modify headers, and HTTP pipeline middleware can replace them entirely.
2. **`oauth2clientcredentials` middleware overwrites the `Authorization` header.** It is designed for service-to-service auth, not for proxying user tokens.
3. **Test auth flows end-to-end in the deployment environment**, not just locally. `dapr run` and `daprd` sidecar containers behave differently due to separate config files and middleware pipelines.
