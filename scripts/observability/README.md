# Observability Stack — Multi-Environment Setup

## Architecture

```
┌─────────────────────┐     ┌─────────────────────┐
│  Dev Server (200)   │     │  Prod Server         │
│                     │     │                      │
│  .NET Services      │     │  .NET Services       │
│    ↓ OTLP           │     │    ↓ OTLP            │
│  OTel Collector     │     │  OTel Collector      │
│  (adds env=dev)     │     │  (adds env=prod)     │
│    ↓ OTLP/HTTP      │     │    ↓ OTLP/HTTP       │
└─────────┬───────────┘     └──────────┬───────────┘
          │                            │
          └──────────┬─────────────────┘
                     ↓
     ┌───────────────────────────────┐
     │  Dev Tools Server (115)       │
     │                               │
     │  Central OTel Collector       │
     │    ↓ Prometheus exporter      │
     │  Prometheus (:9090)           │
     │    ↓ datasource               │
     │  Grafana (grafana.eelkhair.net)│
     └───────────────────────────────┘
```

## Step 1: Deploy observability stack on 115

```bash
# On 192.168.1.115
cd ~/observability
# Copy these files:
#   docker-compose.observability.yml
#   prometheus/prometheus.yml
#   otel-collector/otel-collector-config.yaml

docker compose -f docker-compose.observability.yml up -d
```

## Step 2: Add Prometheus datasource to Grafana

In Grafana (grafana.eelkhair.net):
1. Go to **Connections → Data sources → Add data source**
2. Select **Prometheus**
3. URL: `http://prometheus:9090` (if Grafana is on the same Docker network)
   — or `http://192.168.1.115:9090` (if external)
4. Save & Test

## Step 3: Configure each environment to push OTLP

### Option A: Add OTel Collector as a sidecar (recommended)

Add to `docker-compose.dev.yml` on the dev server (200):

```yaml
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    restart: unless-stopped
    ports:
      - "4317:4317"
      - "4318:4318"
    volumes:
      - ./otel-collector-dev.yaml:/etc/otelcol-contrib/config.yaml:ro
    networks:
      - ai-job-board-net
```

Then add this env var to ALL .NET services:

```yaml
  monolith-api:
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
      # ... existing vars ...
```

### Option B: Point services directly at 115 (simpler, less filtering)

Just add to each service:

```yaml
  monolith-api:
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://192.168.1.115:4317
      - OTEL_RESOURCE_ATTRIBUTES=deployment.environment=dev
```

Option A is better because the local collector:
- Adds the `deployment.environment` label automatically
- Filters Dapr noise before sending (saves bandwidth)
- Buffers/batches (resilient to brief network issues)

## Step 4: Import dashboards

Copy the dashboard JSON files to Grafana:
- `GrafanaDashboards/monolith-overview.json`
- `GrafanaDashboards/ai-service-overview.json`

In Grafana: **Dashboards → Import → Upload JSON file**

Both dashboards have an **Environment** dropdown at the top to switch between dev/prod.

## Environment Label

The `deployment.environment` resource attribute flows through:
1. OTel Collector `resource/env` processor adds it
2. Central collector's `resource_to_telemetry_conversion: enabled` promotes it to a Prometheus label
3. All PromQL queries filter by `deployment_environment=~"$environment"`

## Prometheus Retention

Default: 30 days, max 10GB. Adjust in `docker-compose.observability.yml`:
```
--storage.tsdb.retention.time=30d
--storage.tsdb.retention.size=10GB
```
