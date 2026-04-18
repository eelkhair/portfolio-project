# OTel Collector spanmetrics patch (prod stack on 192.168.1.160)

To enable the Faro RUM Grafana dashboard, your existing OTel Collector
container needs the `spanmetrics` connector. Apply this diff to the inlined
`OTEL_CONFIG` env var in your existing Portainer stack.

## Add a `connectors:` section after `processors:`

```yaml
connectors:
  spanmetrics:
    histogram:
      explicit:
        buckets: [10ms, 50ms, 100ms, 250ms, 500ms, 1s, 2s, 5s, 10s]
    dimensions:
      - name: http.method
      - name: http.route
      - name: deployment.environment
    metrics_flush_interval: 15s
    exemplars:
      enabled: true
```

## Update the `service.pipelines` section

```yaml
service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [filter/dapr, batch]
      # Add `spanmetrics` so traces also feed the connector
      exporters: [otlp/jaeger, otlphttp/seq, spanmetrics]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlphttp/seq]
    metrics:
      # Add `spanmetrics` as a metrics source alongside `otlp`
      receivers: [otlp, spanmetrics]
      processors: [batch]
      exporters: [prometheus]
```

After deploying the patch and restarting the stack, verify:

```bash
curl -s http://localhost:8889/metrics | grep -c traces_span_metrics_calls_total
# > 0 once browser traffic flows through Alloy
#
# (Note: OTel Collector v0.150+ emits the metric as `traces_span_metrics_*`
# with an underscore. Older versions used `traces_spanmetrics_*`.)
```
