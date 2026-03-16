# ADR-017: IntegrationEvents as Shared NuGet Package

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

The distributed platform publishes integration events from the monolith (via transactional outbox) and from microservices (via Dapr pub/sub). Multiple services consume these events:
- **Connector API**: Subscribes to monolith events, fans out to microservices
- **Reverse Connector API**: Subscribes to microservice events, syncs back to monolith
- **AI Service v2**: Subscribes to resume lifecycle events for parsing and embedding

Each consumer needs the event class definitions (record types, property names, `EventType` strings) to deserialize incoming messages. Without a shared source of truth, event schemas drift between producers and consumers — a field rename in the monolith breaks deserialization in the connector API.

Three sharing strategies were considered:
1. **Copy-paste DTOs**: Each service maintains its own event class copies. Fast to start but diverges immediately.
2. **Proto/Avro schema registry**: A formal schema registry with code generation. Robust but heavyweight for the current service count.
3. **Shared NuGet package**: Event contracts published as a versioned package. Consumers take a dependency on the package and get compile-time guarantees.

## Decision

### JobBoard.IntegrationEvents NuGet Package

All integration event contracts are defined in a single project (`services/monolith/Src/Core/JobBoard.IntegrationEvents/`) and published as a NuGet package to a private feed (`https://nuget.eelkhair.net/`).

### Package Structure

**Base interface**:
```csharp
public interface IIntegrationEvent
{
    string EventType { get; }
    string UserId { get; }
}
```

Every event implements `IIntegrationEvent`, ensuring a consistent envelope with a versioned event type string and the originating user ID for audit purposes.

**Event namespaces by domain**:

| Namespace | Events | Direction |
|-----------|--------|-----------|
| `Company` | `CompanyCreatedV1Event`, `CompanyUpdatedV1Event`, `MicroCompanyCreatedV1Event`, `MicroCompanyUpdatedV1Event` | Monolith → Micro, Micro → Monolith |
| `Job` | `JobCreatedV1Event`, `MicroJobCreatedV1Event` | Monolith → Micro, Micro → Monolith |
| `Resume` | `ResumeUploadedV1Event`, `ResumeParsedV1Event`, `ResumeDeletedV1Event` | Monolith → AI Service |
| `Draft` | `DraftSavedV1Event`, `DraftDeletedV1Event` | Micro → Monolith |

### Event Naming Convention

- **`{Domain}{Action}V{version}Event`**: Monolith-originated events (e.g., `CompanyCreatedV1Event`)
- **`Micro{Domain}{Action}V{version}Event`**: Microservice-originated events (e.g., `MicroCompanyCreatedV1Event`)

The `Micro` prefix distinguishes reverse-sync events from forward-sync events. This prevents topic collision and makes the data flow direction explicit in code.

### Lightweight vs. Full Payload Events

Two event styles coexist:

- **Lightweight events** (e.g., `CompanyCreatedV1Event`): Contains only identifiers (`CompanyUId`, `IndustryUId`, `AdminUId`). The consumer fetches full details from the source system. Used when the consumer needs fresh data and the source is the authority.
- **Full payload events** (e.g., `MicroCompanyCreatedV1Event`): Contains all fields needed for sync (`Name`, `CompanyEmail`, `AdminFirstName`, etc.). Used for reverse sync where the consumer (monolith) shouldn't call back to microservices.

### Shared DTOs for Complex Payloads

The package also includes shared DTOs for structured content that crosses service boundaries:

- `ResumeParsedContentResponse`: Full parsed resume (skills, work history, education, certifications, projects, summary)
- `ResumeWorkHistoryDto`, `ResumeEducationDto`, `ResumeCertificationDto`, `ResumeProjectDto`

These DTOs are consumed by both the monolith (which stores parsed content) and the AI service (which fetches it for embedding). Sharing them in the package eliminates DTO duplication.

### Package Hierarchy

```
JobBoard.IntegrationEvents (v1.0.11)
    ↑ ProjectReference
JobBoard.Contracts / JobBoard.Monolith.Contracts (v1.0.3)
    ↑ PackageReference
connector-api, reverse-connector-api, ai-service.v2, microservices
```

`JobBoard.Contracts` extends `IntegrationEvents` with API request/response DTOs and has a `ProjectReference` to IntegrationEvents (since they're in the same solution). External services consume `JobBoard.Monolith.Contracts` as a NuGet package, which transitively includes IntegrationEvents.

### Version Management

The package version is incremented manually in the `.csproj` (`<Version>1.0.11</Version>`). After incrementing:
1. `dotnet pack` produces the `.nupkg`
2. `dotnet nuget push` publishes to the private feed
3. Consumer services update their `PackageReference` version

This is intentionally manual — event schema changes should be deliberate, not automated. A version bump signals that consumers need to update and potentially handle new fields.

## Consequences

### Positive

- **Compile-time contract enforcement**: If a producer changes an event's record signature, consumers fail to compile until they update — no runtime deserialization surprises.
- **Single source of truth**: Event types, property names, and `EventType` strings are defined once. The `Micro` prefix convention makes flow direction unambiguous.
- **Versioned evolution**: The `V1` suffix in event names allows introducing `V2` events alongside `V1` without breaking existing consumers. Old and new versions can coexist during migration.
- **Shared DTOs reduce duplication**: Complex structures like `ResumeParsedContentResponse` are defined once and used by multiple services.

### Tradeoffs

- **Coupling via package**: All consumers depend on the same package. A breaking change requires updating all consumers simultaneously. For this portfolio's service count (~5 consumers), this is manageable.
- **No schema evolution tooling**: Unlike Avro or Protobuf, C# records don't have built-in backward/forward compatibility guarantees. Adding a required property to an event is a breaking change. Mitigation: new fields should be optional (`string?`, `Guid?`).
- **Manual versioning**: No CI automation for pack/push. This is acceptable for a portfolio project but would need automation at production scale.
- **Monolith-centric location**: The IntegrationEvents project lives inside the monolith solution, even though it's consumed by independent services. This is a pragmatic choice — the monolith is where most events originate — but could create confusion about ownership.

## Implementation Notes

- All events use C# `record` types for immutability and value equality.
- `EventType` strings follow the pattern `{origin}.{domain}.{action}.{version}` (e.g., `company.created.v1`, `micro.company.created.v1`).
- The `UserId` property tracks the original actor for audit trail propagation through the event pipeline.
- The NuGet package source mapping in `Nuget.Config` ensures `JobBoard.*` packages resolve from the private feed while everything else comes from nuget.org.
- The outbox publisher serializes events using `System.Text.Json` with the concrete event type (`JsonSerializer.Serialize(event, event.GetType())`), preserving all record properties in the JSON payload.
