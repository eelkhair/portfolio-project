# ADR-019: EF Core Dual-ID Pattern with Sequence-Based Generation

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

Every domain entity needs two identifiers:
1. **An integer primary key** for database performance — clustered index, efficient joins, compact foreign keys.
2. **A GUID public identifier** for API responses — non-guessable, safe to expose, stable across environments.

Using only GUIDs as primary keys causes index fragmentation and wider foreign key columns. Using only integers exposes sequential IDs in URLs, enabling enumeration attacks and leaking information about record counts.

Three approaches were considered:
1. **GUID-only**: Simple but fragments clustered indexes and wastes storage on foreign keys.
2. **Integer-only with obfuscation**: Hashids or similar encoding. Adds a translation layer to every API response/request and breaks if the encoding changes.
3. **Dual-ID**: Integer for internal use, GUID for external use. Two columns per entity but clean separation of concerns.

## Decision

### Dual-ID Base Entity

Every domain entity inherits from `BaseEntity`, which provides both identifiers:

**Monolith** (`JobBoard.Domain`):
```csharp
public abstract class BaseEntity
{
    public int InternalId { get; set; }  // DB primary key
    public Guid Id { get; set; }         // Public API identifier
}
```

**Microservices** (`Elkhair.Dev.Common`):
```csharp
public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }          // DB primary key
    public Guid UId { get; set; }        // Public API identifier
}
```

The property naming differs between monolith and microservices (a conscious choice — each codebase uses the convention that felt natural when it was written). The semantic meaning is identical: integer for internal, GUID for external.

### SQL Server Sequences (Monolith)

The monolith uses SQL Server sequences for integer ID generation rather than `IDENTITY` columns:

```csharp
builder.Property<int>("InternalId")
    .ValueGeneratedOnAdd()
    .HasDefaultValueSql($"NEXT VALUE FOR {table}_Sequence");
```

Each entity gets its own sequence (e.g., `Companies_Sequence`, `Jobs_Sequence`), declared dynamically in `OnModelCreating`:

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    var tableName = entityType.GetTableName();
    modelBuilder.HasSequence<int>($"{tableName}_Sequence");
}
```

Sequences were chosen over `IDENTITY` because:
- **Atomic pair generation**: The `GetNextValueFromSequenceAsync` method fetches the next sequence value and generates a `Guid.CreateVersion7()` in the same operation, guaranteeing the `(InternalId, Id)` pair is assigned together before `SaveChangesAsync`.
- **Pre-insert knowledge**: The integer ID is known before the `INSERT` statement executes. This enables patterns like setting up child entities with parent IDs in the same transaction without requiring a round-trip to read the generated identity.
- **No SCOPE_IDENTITY() dependency**: Sequences work predictably with batch inserts and temporal tables.

### GUID Generation Strategy

| System | Strategy | Benefit |
|--------|----------|---------|
| Monolith | `Guid.CreateVersion7()` (code-side) | Time-ordered, clustered-index friendly, generated before insert |
| Microservices | `newsequentialid()` (SQL Server function) | Sequential for index performance, no application code needed |

The monolith uses `CreateVersion7()` because the GUID is paired with the sequence value in application code. Microservices use `newsequentialid()` as a simpler default since they don't need pre-insert GUID knowledge.

### Auditable Entity Extension

`BaseAuditableEntity` adds audit fields:

**Monolith**:
```csharp
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
```

**Microservices** (extends with soft delete):
```csharp
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }  // Nullable
    public string UpdatedBy { get; set; } = string.Empty;
    public string RecordStatus { get; set; } = RecordStatuses.Active;
}
```

The microservices' `BaseAuditableEntity` adds `RecordStatus` with a global query filter (`HasQueryFilter(e => e.RecordStatus == "Active")`) for soft deletes. The monolith handles record status at the domain level rather than via EF filters.

### Audit Field Population

The monolith's `SaveChangesAsync(string userId, CancellationToken)` auto-populates audit fields by scanning `ChangeTracker.Entries<BaseAuditableEntity>()`:
- **Added**: Sets `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` to current UTC time and user ID
- **Modified**: Updates only `UpdatedAt` and `UpdatedBy`

This ensures audit fields are always populated consistently, regardless of which handler creates or modifies the entity.

### Entity Configuration Convention

Both monolith and microservices use extension methods for consistent configuration:

**Monolith** (`DataExtensions.ConfigureBusinessEntity<T>()`):
- Sets `InternalId` as primary key with sequence default
- Marks `Id` as required, `ValueGeneratedNever`
- Enables SQL Server temporal tables (`IsTemporal()`)

**Microservices** (`DataExtensions.ConfigureBaseEntity<T>()`):
- Sets `UId` default to `newsequentialid()`
- Enables SQL Server temporal tables (`IsTemporal()`)

Both systems enable temporal tables on all business entities, providing point-in-time query support and historical audit trails at the database level.

## Consequences

### Positive

- **Performant indexes**: Integer primary keys produce compact, sequential clustered indexes. GUID uniqueness constraints are non-clustered secondary indexes.
- **Secure API surface**: Only GUIDs appear in API responses and URLs. Sequential integers never leave the service boundary.
- **Pre-insert ID knowledge**: The sequence-based approach lets the monolith assign both IDs before inserting, enabling complex aggregate creation in a single transaction.
- **Temporal audit trail**: SQL Server temporal tables on all entities provide automatic history tracking without application-level event sourcing.
- **Consistent audit fields**: Auto-population in `SaveChangesAsync` eliminates the risk of missing `CreatedBy`/`UpdatedBy` values.

### Tradeoffs

- **Two naming conventions**: The monolith uses `InternalId`/`Id` while microservices use `Id`/`UId`. This creates a translation burden when mapping events between systems (the IntegrationEvents package uses `UId` for GUIDs in all events). Unifying the naming was considered but rejected to avoid a large migration in the monolith.
- **Sequence per table**: Each entity creates a SQL Server sequence object. For a small entity count (~10), this is negligible. At scale, the number of sequence objects could become a management concern.
- **Two columns per entity**: Every table has both `InternalId`/`Id` (or `Id`/`UId`), adding 20 bytes per row. The storage overhead is marginal compared to the benefits.
- **Soft delete divergence**: Microservices use `RecordStatus` with global query filters; the monolith does not. This means a "deleted" record in microservices is still in the database (filtered), while monolith deletions are hard deletes or domain-status changes.

## Implementation Notes

- **`Guid.CreateVersion7()`** (introduced in .NET 9) generates time-ordered GUIDs that are clustered-index friendly. Earlier versions used `Guid.NewGuid()` which required a `newsequentialid()` default instead.
- **Temporal tables**: `IsTemporal()` in both `ConfigureBusinessEntity` and `ConfigureBaseEntity` adds `SysStartTime`/`SysEndTime` columns automatically via EF Core migration.
- **Outbox exclusion**: The monolith's sequence-generation loop in `OnModelCreating` explicitly skips entities in the `"outbox"` schema to avoid creating sequences for infrastructure tables.
- **`GetNextValueFromSequenceAsync`**: Uses raw ADO.NET (`command.ExecuteScalarAsync`) within the current EF Core transaction to fetch the next sequence value, ensuring transactional consistency.
