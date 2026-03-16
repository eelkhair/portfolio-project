# ADR-014: Resume Embedding Pipeline (RAG)

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

The platform allows applicants to upload resumes. To enable AI-powered features — resume summarization, skill extraction, and future semantic job matching — the system needs to parse resume content and generate vector embeddings for retrieval-augmented generation (RAG).

Key requirements:
- **Asynchronous processing**: Resume parsing involves LLM calls that take 10-30 seconds. This must not block the upload request.
- **Multi-phase pipeline**: Parsing (text extraction + LLM structuring) and embedding (vector generation + storage) are distinct concerns with different failure modes.
- **Lifecycle management**: When a resume is deleted, its embedding must also be removed.
- **Observability**: Each pipeline stage must be traceable as part of the same distributed operation that began with the upload.

Two pipeline architectures were considered:
1. **Synchronous in-process**: Parse and embed in the upload handler. Simple but blocks the user for 30+ seconds and couples the monolith to AI infrastructure.
2. **Event-driven multi-stage**: The monolith publishes lifecycle events, and a dedicated AI service handles parsing and embedding asynchronously. Decoupled and resilient but requires reliable event delivery.

## Decision

### Three-Stage Event-Driven Pipeline

The pipeline uses the monolith's transactional outbox ([ADR-003](./ADR-003-Transactional-Outbox.md)) to publish resume lifecycle events, which the AI service v2 processes asynchronously:

```
Upload → ResumeUploadedV1Event → Parse → ResumeParsedV1Event → Embed
                                                                  ↓
Delete → ResumeDeletedV1Event → Delete Embedding          pgvector store
```

### Stage 1: Parse (ResumeUploadedV1Event)

When the monolith stores a resume blob and publishes `ResumeUploadedV1Event`, the AI service:

1. **Downloads** the resume from Azure Blob Storage (container: `resumes`)
2. **Extracts text** using `ResumeTextExtractor` (one-time extraction reused across all prompts)
3. **Parses in two phases** via LLM function calling:
   - **Phase 1 (Quick)**: Contact info, summary, skills — low-latency extraction
   - **Phase 2 (Detailed)**: Work history, education, certifications, projects — sequential with up to 2 retries per section
4. **Callbacks to monolith**: Each parsed section triggers `NotifySectionParsedAsync()`, enabling streaming progress in the frontend. On completion, `NotifyAllSectionsCompletedAsync()` is called. On failure, `NotifyResumeParseFailedAsync()` reports the error reason.

The multi-phase approach allows the UI to display partial results progressively rather than waiting for all sections to complete.

### Stage 2: Embed (ResumeParsedV1Event)

After the monolith stores all parsed sections and publishes `ResumeParsedV1Event`, the AI service:

1. **Fetches** the full `ResumeParsedContentResponse` from the monolith (canonical parsed data)
2. **Generates batch embeddings** for three text representations:
   - `full`: All sections concatenated — for broad semantic matching
   - `skills`: Skills section only — for targeted skill-based queries
   - `experience`: Work history with descriptions — for experience-level matching
3. **Upserts** a `ResumeEmbedding` record in PostgreSQL with pgvector

### Stage 3: Delete (ResumeDeletedV1Event)

When a resume is deleted from the monolith, `ResumeDeletedV1Event` triggers removal of the corresponding `ResumeEmbedding` record. Graceful handling: no error if the embedding doesn't exist.

### pgvector for Vector Storage

PostgreSQL with the pgvector extension was chosen over dedicated vector databases:

| Option | Pros | Cons |
|--------|------|------|
| **pgvector** | Runs in existing PostgreSQL, SQL joins with relational data, mature ecosystem | Lower QPS at scale than dedicated solutions |
| **Pinecone/Weaviate** | Purpose-built, high QPS, managed | Additional service, cost, no SQL joins |
| **Redis VSS** | Already in stack (Dapr state) | Limited query capabilities, no persistence guarantees |

pgvector was chosen because:
- The AI service already uses PostgreSQL for conversation and entity storage
- Resume count is bounded (thousands, not millions) — pgvector handles this without performance issues
- Vector queries can join with relational metadata (user, company) in a single SQL query
- No additional infrastructure to operate

### Embedding Configuration

- **Model**: `text-embedding-3-small` (OpenAI) — 1536 dimensions
- **Distance metric**: Cosine distance (`<=>` operator)
- **Provider abstraction**: `IEmbeddingGenerator<string, Embedding<float>>` from `Microsoft.Extensions.AI` allows swapping to Azure OpenAI or other providers via configuration
- **Section-level columns**: The `resume_embeddings` table includes optional `skills_vector`, `experience_vector`, and additional columns (1536-dim each) for granular matching

### Idempotency

All three event handlers use the same Redis-backed idempotency pattern as the Connector API — state keys with `{action}:{idempotencyKey}`, 120-second processing TTL, 7-day completion TTL. The `DaprInternal` auth policy ensures only the Dapr sidecar can invoke these endpoints.

## Consequences

### Positive

- **Non-blocking uploads**: The user receives an immediate response; parsing happens in the background with streaming progress via SignalR notifications ([ADR-010](./ADR-010-Real-Time-AI-Notifications-SignalR.md)).
- **Independent scaling**: The AI service can scale independently of the monolith to handle parsing load spikes (e.g., bulk resume imports).
- **Multi-representation embeddings**: Storing full, skills, and experience vectors separately enables targeted retrieval — a job search can weight skills higher than general experience.
- **Traceable end-to-end**: The outbox processor restores `TraceParent` from the original upload request, and each pipeline stage adds child spans, producing a complete Jaeger trace from upload through embedding.
- **Resilient**: If the AI service is down, events queue in RabbitMQ. If a parsing phase fails, the callback reports the failure and the event can be retried.

### Tradeoffs

- **Two round-trips to monolith**: Stage 1 downloads the blob via the monolith's storage. Stage 2 fetches parsed content from the monolith rather than passing it through the event. This keeps events lightweight but adds latency.
- **LLM dependency**: Parsing quality depends on the LLM provider. Model changes or outages affect parsing accuracy. The retry mechanism (2 retries per section) mitigates transient failures.
- **Schema evolution**: Adding new parsed sections (e.g., `projects_vector`) requires both a migration on the AI service's database and updates to the embedding logic. The column-per-section approach trades flexibility for query simplicity.

## Implementation Notes

- **Blob storage**: `AzureBlobStorageService` wraps `BlobServiceClient` with a `ConcurrentDictionary<string, bool>` cache to avoid 409 Conflict errors on container creation.
- **LLM parsing**: Uses `Microsoft.Extensions.AI` with `FunctionInvokingChatClient` for structured extraction. Each section has a dedicated prompt optimized for that content type.
- **Database**: `resume_embeddings` table with `vector(1536)` columns. EF Core maps `Vector` type from `Pgvector.EntityFrameworkCore`.
- **Event contracts**: `ResumeUploadedV1Event`, `ResumeParsedV1Event`, `ResumeDeletedV1Event` are defined in the shared `JobBoard.IntegrationEvents` package ([ADR-017](./ADR-017-IntegrationEvents-Shared-NuGet-Package.md)).
- **Shared DTOs**: `ResumeParsedContentResponse` and related DTOs (`ResumeWorkHistoryDto`, `ResumeEducationDto`, etc.) live in the IntegrationEvents package to avoid duplication between the monolith and AI service.
