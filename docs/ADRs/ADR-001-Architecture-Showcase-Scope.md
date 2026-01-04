# ADR-001: Portfolio Architecture Scope (Monolith + Microservices + Strangler-Fig)

- **Status:** Accepted
- **Date:** 2026-01-03

## Context

This repository is a portfolio/reference architecture meant to demonstrate real-world engineering and architectural decision-making in a single, reviewable codebase. In professional environments, teams often evolve from a monolith to services (or mix both) while maintaining delivery speed and reliability.

A portfolio that only demonstrates one style (only a monolith or only microservices) can hide important tradeoffs:
- operability vs simplicity
- consistency vs autonomy
- migration strategy vs greenfield purity

## Decision

We will intentionally include **three architectural approaches** within the same domain:

1. **Clean Architecture Monolith** to demonstrate maintainability, DDD boundaries, CQRS, and operational readiness.
2. **Microservices** to demonstrate service decomposition and distributed workflows.
3. **Connector API (Strangler-Fig)** to demonstrate incremental migration and integration patterns between legacy and new capabilities.

The goal is not minimal complexity—it is to showcase **tradeoffs and evolution**.

## Consequences

### Positive
- Demonstrates breadth: design, migration, integration, distributed concerns.
- Enables “compare and contrast” discussions in interviews and consulting.
- Provides a realistic narrative for system evolution over time.

### Negative / Tradeoffs
- Increased repo complexity and cognitive load.
- Some duplication is acceptable to keep each approach clear.
- Requires strong documentation (READMEs + ADRs) to guide reviewers.

## Notes

Where useful, each approach should remain independently understandable:
- Clear boundaries, naming, and folders
- Consistent observability/health conventions
- Minimal coupling across approaches beyond intentional integration points
