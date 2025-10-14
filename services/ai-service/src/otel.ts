// src/otel.ts
import { NodeSDK } from '@opentelemetry/sdk-node';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes as SRA } from '@opentelemetry/semantic-conventions';
import { CompositePropagator, W3CTraceContextPropagator } from '@opentelemetry/core';
import { B3Propagator } from '@opentelemetry/propagator-b3';
import { AsyncLocalStorageContextManager } from '@opentelemetry/context-async-hooks';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { ZipkinExporter } from '@opentelemetry/exporter-zipkin';
import { HttpInstrumentation } from '@opentelemetry/instrumentation-http';
import { FastifyInstrumentation } from '@opentelemetry/instrumentation-fastify';

const resource = new Resource({
    [SRA.SERVICE_NAME]: process.env.OTEL_SERVICE_NAME ?? 'ai-service',
    [SRA.SERVICE_NAMESPACE]: process.env.OTEL_SERVICE_NAMESPACE ?? 'portfolio',
    [SRA.DEPLOYMENT_ENVIRONMENT]: process.env.NODE_ENV ?? 'dev',
});

const otlpExporter = new OTLPTraceExporter({
    url: process.env.OTEL_EXPORTER_OTLP_TRACES_ENDPOINT || 'http://localhost:4318/v1/traces',
});
const zipkinExporter = new ZipkinExporter({
    url: process.env.OTEL_EXPORTER_ZIPKIN_ENDPOINT || 'http://localhost:9411/api/v2/spans',
});

export const sdk = new NodeSDK({
    resource,
    spanProcessor: new BatchSpanProcessor(otlpExporter),
    instrumentations: [
        new HttpInstrumentation(),          // don’t filter anything yet
        new FastifyInstrumentation({}),     // no wrapRoutes on 0.50.x
    ],
    contextManager: new AsyncLocalStorageContextManager(),
    textMapPropagator: new CompositePropagator({
        propagators: [new W3CTraceContextPropagator(), new B3Propagator()],
    }),
});

export async function startOtel() {
    await sdk.start();
    // add Zipkin as a second exporter after start
    const tp: any = (sdk as any)['_tracerProvider'];
    tp?.addSpanProcessor(new BatchSpanProcessor(zipkinExporter));
}

export async function stopOtel() {
    await sdk.shutdown();
}
