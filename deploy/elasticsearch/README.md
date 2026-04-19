# Elasticsearch ingest pipelines

## `fe-otel-to-serilog.json`

Ingest pipeline applied to the `fe-logs` index. Normalizes frontend OTel log/span docs
(from Faro via Alloy → OTel Collector) into the Serilog-style shape the Grafana
**Find by Trace Id** dashboard expects — so FE spans and FE `pushLog` events sit
next to backend Serilog logs in the same table with the same column layout.

### What it does

- Renames root-level `TraceId` / `SpanId` / `ParentSpanId` / `ApplicationName` →
  `fields.traceId` / `fields.spanId` / `fields.parentSpanId` / `fields.ApplicationName`
- Renames `Body` → `message`, `SeverityText` → `level`, `Timestamp` → `@timestamp`
- Spans use `Name` → `message` (fallback when `Body` isn't set), `Kind` → `fields.Kind`,
  `StartTimestamp` → `@timestamp` (fallback)
- Flattens `Attributes.*` into `fields.*` for the FE-specific attributes (`geo.*`,
  `http.*`, `app_environment`, `app_name`, `level`, etc.) — guarded so root-level
  renames win when both are present
- **Strips the OTLP envelope** that only exists on span docs (`Resource.*`,
  `Scope.*`, `TraceStatus`, `Duration`, `EndTimestamp`, `Link`, `Links`, `Events`,
  `session.id`, `SeverityNumber`) — these conflict with the flat log mapping that
  the Serilog backend uses, and ES rejects docs with an `illegal_argument_exception`
  when a field like `Resource.deployment.environment` is a string on a log doc and
  an object on a span doc

### Applying

Run once against any fresh Elasticsearch cluster, before the OTel Collector's
`elasticsearch/fe` exporter starts writing to `fe-logs`:

```bash
curl -X PUT -H 'Content-Type: application/json' \
  http://ELASTICSEARCH_HOST:9200/_ingest/pipeline/fe-otel-to-serilog \
  --data-binary @fe-otel-to-serilog.json
```

The collector references this pipeline by name in `deploy/alloy/otel-collector-patched.yaml`:

```yaml
elasticsearch/fe:
  endpoints: [http://HOST:9200]
  logs_index: fe-logs
  traces_index: fe-logs
  pipeline: fe-otel-to-serilog
```

Pipeline changes are applied with the same PUT — Elasticsearch replaces the
existing definition. No restart needed.
