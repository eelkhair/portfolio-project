// smoke-otel.mjs
import { NodeSDK } from '@opentelemetry/sdk-node';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes as SRA } from '@opentelemetry/semantic-conventions';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { ZipkinExporter } from '@opentelemetry/exporter-zipkin';
import { AsyncLocalStorageContextManager } from '@opentelemetry/context-async-hooks';
import { trace } from '@opentelemetry/api';

const otlp = new OTLPTraceExporter({
    url: process.env.OTEL_EXPORTER_OTLP_TRACES_ENDPOINT || 'http://localhost:4318/v1/traces',
});
const zipkin = new ZipkinExporter({
    url: process.env.OTEL_EXPORTER_ZIPKIN_ENDPOINT || 'http://localhost:9411/api/v2/spans',
});

const sdk = new NodeSDK({
    resource: new Resource({
        [SRA.SERVICE_NAME]: 'ai-service-smoke',
        [SRA.DEPLOYMENT_ENVIRONMENT]: 'dev',
    }),
    spanProcessor: new BatchSpanProcessor(otlp),
    contextManager: new AsyncLocalStorageContextManager(),
});

(async () => {
    await sdk.start();
    // also export to Zipkin
    const tp = sdk._tracerProvider; // ok for a quick smoke
    tp?.addSpanProcessor(new BatchSpanProcessor(zipkin));

    const tracer = trace.getTracer('smoke');
    const span = tracer.startSpan('smoke-span');
    await new Promise(r => setTimeout(r, 50));
    span.end();

    // give exporters time to flush
    await new Promise(r => setTimeout(r, 500));
    await sdk.shutdown();
    console.log('✅ smoke test done');
})();
