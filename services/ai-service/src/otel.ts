// src/otel.ts (ESM/TS)
import { NodeSDK } from '@opentelemetry/sdk-node';
import { BatchSpanProcessor, type SpanProcessor, type ReadableSpan } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { ZipkinExporter } from '@opentelemetry/exporter-zipkin';
import { defaultResource, resourceFromAttributes } from '@opentelemetry/resources';
import {
    SEMRESATTRS_SERVICE_NAME,
    SEMRESATTRS_SERVICE_NAMESPACE,
    SEMRESATTRS_DEPLOYMENT_ENVIRONMENT,
} from '@opentelemetry/semantic-conventions';

import { HttpInstrumentation } from '@opentelemetry/instrumentation-http';

import { AsyncLocalStorageContextManager } from '@opentelemetry/context-async-hooks';
import { CompositePropagator, W3CTraceContextPropagator } from '@opentelemetry/core';
import { B3Propagator } from '@opentelemetry/propagator-b3';

/* ------------------------ helpers: multi + filtering processors ------------------------ */

class MultiSpanProcessor implements SpanProcessor {
    constructor(private readonly processors: SpanProcessor[]) {}

    onStart(span: any, ctx: any) {
        for (const p of this.processors) p.onStart?.(span, ctx);
    }
    onEnd(span: ReadableSpan) {
        for (const p of this.processors) p.onEnd(span);
    }
    forceFlush(): Promise<void> {
        return Promise.all(this.processors.map(p => p.forceFlush())).then(() => undefined);
    }
    shutdown(): Promise<void> {
        return Promise.all(this.processors.map(p => p.shutdown())).then(() => undefined);
    }
}

class FilteringSpanProcessor implements SpanProcessor {
    constructor(private readonly delegate: SpanProcessor, private readonly shouldExport: (span: ReadableSpan) => boolean) {}

    onStart(span: any, ctx: any) {
        this.delegate.onStart?.(span, ctx);
    }
    onEnd(span: ReadableSpan) {
        if (this.shouldExport(span)) this.delegate.onEnd(span);
    }
    forceFlush(): Promise<void> {
        return this.delegate.forceFlush();
    }
    shutdown(): Promise<void> {
        return this.delegate.shutdown();
    }
}

/* ----------------------------- resource (service identity) ----------------------------- */

const resource =
    defaultResource().merge(
        resourceFromAttributes({
            [SEMRESATTRS_SERVICE_NAME]: process.env.OTEL_SERVICE_NAME ?? 'ai-service',
            [SEMRESATTRS_SERVICE_NAMESPACE]: process.env.OTEL_SERVICE_NAMESPACE ?? 'portfolio',
            [SEMRESATTRS_DEPLOYMENT_ENVIRONMENT]: process.env.NODE_ENV ?? 'dev',
        })
    );

/* ------------------------------------ exporters --------------------------------------- */

// Jaeger / OTel collector via OTLP/HTTP
const otlpExporter = new OTLPTraceExporter({
    url: process.env.OTEL_EXPORTER_OTLP_TRACES_ENDPOINT || 'http://localhost:4318/v1/traces',
});

// Zipkin
const zipkinExporter = new ZipkinExporter({
    url: process.env.OTEL_EXPORTER_ZIPKIN_ENDPOINT || 'http://localhost:9411/api/v2/spans',
});

// Fan out to both backends
const multi = new MultiSpanProcessor([
    new BatchSpanProcessor(otlpExporter),
    new BatchSpanProcessor(zipkinExporter),
]);

/* ------------------- drop noisy health/Dapr spans before exporting --------------------- */

const IGNORE_PATTERNS = [
    /^\/(livez|readyz|healthzEndpoint)$/i,                 // your health endpoints (incoming)
    /^\/v1\.0\/(state|secrets|publish)\b/i,               // Dapr HTTP API (outgoing)
    /\/v1\.0\/publish\/[^/]+\/healthCheckTopic\b/i,       // the specific pubsub health topic
];

function shouldExport(span: ReadableSpan): boolean {
    // pull out anything we can treat as a path
    const route = String(span.attributes['http.route'] ?? '');
    const target = String(span.attributes['http.target'] ?? '');
    const urlStr = String(span.attributes['http.url'] ?? span.attributes['url.full'] ?? '');

    let path = route || target;
    if (!path && urlStr) {
        try {
            path = new URL(urlStr).pathname || '';
        } catch {
            // ignore parse errors
        }
    }

    if (path && IGNORE_PATTERNS.some(rx => rx.test(path))) return false;
    return true;
}

const filtering = new FilteringSpanProcessor(multi, shouldExport);

/* --------------------------------- SDK initialization --------------------------------- */

const httpInstrumentation = new HttpInstrumentation({
    // no version-specific ignore options here — we filter in the processor
});

const sdk = new NodeSDK({
    resource,
    spanProcessor: filtering,
    instrumentations: [
        httpInstrumentation,
        // If you use node's global fetch (undici), add this for better client spans:
        // new (await import('@opentelemetry/instrumentation-undici')).UndiciInstrumentation(),
    ],
    contextManager: new AsyncLocalStorageContextManager(),
    textMapPropagator: new CompositePropagator({
        propagators: [new W3CTraceContextPropagator(), new B3Propagator()],
    }),
});

await sdk.start();

// graceful shutdown
const shutdown = async () => {
    try {
        await sdk.shutdown();
    } finally {
        process.exit(0);
    }
};
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
