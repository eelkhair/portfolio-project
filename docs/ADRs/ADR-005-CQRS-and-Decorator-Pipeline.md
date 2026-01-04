# ADR-005: CQRS + Decorator Pipeline (Instead of Framework-Heavy Mediator Coupling)

- **Status:** Accepted
- **Date:** 2026-01-03

## Context

CQRS can improve maintainability by separating reads from writes and allowing each to evolve independently. Many projects implement CQRS with MediatR. In this portfolio, we want to demonstrate:
- CQRS as a pattern, not a library dependency
- explicit cross-cutting concerns (validation, transactions, observability)
- clean, testable application boundaries

## Decision

We implement CQRS with:
- Explicit `Command` / `Query` contracts
- Handlers that focus on business behavior
- A **decorator pipeline** around handlers for cross-cutting concerns:
  - Validation
  - Transactions
  - Observability
  - (Optional) Idempotency / retries where applicable

This keeps:
- the application layer explicit and portable
- cross-cutting behavior composable and testable

## Consequences

### Positive
- Reduced framework lock-in; clearer intent.
- Cross-cutting concerns remain consistent and centrally managed.
- Easier to test each decorator in isolation.

### Negative / Tradeoffs
- Slightly more scaffolding than “just use MediatR”.
- Requires conventions and documentation to stay consistent.

## Implementation Notes

- Decorators should be ordered intentionally (e.g., Validation → Transaction → Observability around core handler).
- The handler contracts should remain stable and minimal.
