# ADR-002: Dapr Usage Boundaries and Azure Transition Strategy

- **Status:** Accepted
- **Date:** 2026-01-03

## Context

This portfolio currently uses **Dapr** in both the monolith and microservices to provide:
- Pub/Sub for integration events
- Configuration and secrets via Redis / HashiCorp Vault
- Local development parity across environments

This choice optimizes for **local development, portability, and architectural demonstration**.

However, the **target production environment is Azure**, where first‑party managed services provide stronger guarantees, operational simplicity, and enterprise alignment.

The portfolio must therefore demonstrate:
- Pragmatic use of Dapr today
- A **clear and intentional exit strategy** when deploying to Azure

## Decision

### Current State (Local / Portfolio Environment)

Dapr is used in the **monolith and microservices** to:
- Publish integration events
- Access configuration and secrets
- Enable local real‑time configuration notifications

Redis and Vault are used **only as development-time backing stores**, not as long-term production choices.

### Azure Target State (Planned Replacement)

When deployed to Azure, Dapr building blocks will be **progressively replaced** with native Azure services:

| Capability | Local / Portfolio | Azure Target |
|-----------|------------------|-------------|
| Configuration | Dapr + Redis | **Azure App Configuration** |
| Secrets | Dapr + Vault | **Azure Key Vault** |
| Pub/Sub / Events | Dapr Pub/Sub | **Azure Event Grid** |
| Real-time Config Updates | Dapr Pub/Sub | **Azure SignalR Service** |

Dapr will **not** be a hard dependency in the Azure runtime path.

## Rationale

This approach allows the portfolio to demonstrate:

- Tool pragmatism: use Dapr where it accelerates development
- Cloud readiness: show clear alignment with Azure-native services
- Architectural maturity: design for **replaceable infrastructure**
- Realistic enterprise migration patterns

Dapr is treated as an **integration abstraction**, not a platform lock‑in.

## Consequences

### Positive
- Strong local developer experience without Azure dependency
- Clear migration path to managed Azure services
- Demonstrates real-world cloud transition thinking
- Avoids Redis/Vault being perceived as production defaults

### Tradeoffs
- Requires dual documentation (local vs Azure)
- Some abstraction layers must remain intentionally thin
- Additional testing needed when swapping providers

## Implementation Notes

- Configuration access is abstracted behind application interfaces
- Event publishing uses domain/integration event contracts independent of transport
- Real-time configuration updates are designed as optional infrastructure concerns
- Azure-specific wiring will live in deployment/IaC layers (Bicep/Terraform)

## Portfolio Note

This ADR intentionally documents **future-state architecture**, not only current implementation.

That forward-looking clarity is part of the portfolio’s purpose.
