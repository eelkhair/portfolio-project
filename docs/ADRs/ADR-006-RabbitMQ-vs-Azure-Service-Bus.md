# ADR-006: RabbitMQ vs Azure Service Bus for Messaging

- **Status:** Accepted
- **Date:** 2026-01-03

## Context

This portfolio demonstrates event-driven communication across a monolith, microservices, and a connector API.  
For local development and portability, **RabbitMQ** is commonly used.  
For Azure production environments, **Azure Service Bus** is a first-party managed messaging service.

The goal is to show **intentional selection** of messaging infrastructure based on environment and requirements, while keeping application code insulated from transport-specific concerns.

## Decision

### Local / Portfolio Environment

We use **RabbitMQ** as the primary message broker for local development and demos:

- Easy local setup (Docker)
- Clear visibility into queues, exchanges, and routing
- Supports pub/sub, fanout, and work queues
- Widely understood in the industry
- Works well with Dapr pub/sub and direct client libraries

### Azure Target Environment

For Azure deployments, **Azure Service Bus** is the preferred messaging backbone:

- Fully managed, enterprise-grade messaging
- Built-in retries, dead-letter queues, and durability
- Tight integration with Azure security, networking, and monitoring
- Predictable operational model for enterprise teams

RabbitMQ is **not** positioned as the default production choice in Azure for this portfolio.

## Rationale

This split allows the portfolio to demonstrate:

- Local-first development without cloud dependency
- Cloud-native production alignment
- Replaceable infrastructure via abstractions
- Realistic enterprise migration patterns

Messaging is treated as **infrastructure**, not a domain dependency.

## Comparison Summary

| Capability | RabbitMQ | Azure Service Bus |
|----------|---------|------------------|
| Hosting Model | Self-managed | Fully managed |
| Local Dev | Excellent | Poor (cloud-only) |
| Azure Integration | Moderate | Native |
| Throughput Control | High | High |
| Dead-Lettering | Yes | Yes (first-class) |
| Ops Overhead | Higher | Lower |
| Enterprise Governance | Manual | Built-in |

## Consequences

### Positive
- Clean separation between domain logic and transport
- Realistic dev-to-prod story
- Demonstrates operational maturity
- Avoids cloud lock-in during development

### Tradeoffs
- Two transports must be documented
- Requires contract stability and idempotent consumers
- Additional testing when switching providers

## Implementation Notes

- Messaging is accessed via application-level interfaces
- Events use stable schemas and identifiers
- Consumers are idempotent
- Retry and dead-letter behavior is explicit and observable
- Azure Service Bus wiring lives in infrastructure / IaC layers

## Portfolio Note

This ADR exists to demonstrate **decision-making**, not to declare a single “best” broker.

The ability to justify *why* and *when* each option is used is the primary signal.
