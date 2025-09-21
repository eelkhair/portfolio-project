// src/otel.ts
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor} from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes as SRA } from '@opentelemetry/semantic-conventions';
import {environment} from './environments/environment';
import {ZipkinExporter} from '@opentelemetry/exporter-zipkin';
import { W3CTraceContextPropagator } from '@opentelemetry/core';

const exporter = new OTLPTraceExporter({ url: environment.otel });
const zipkinExporter = new ZipkinExporter({
  url: environment.otelZipkin,
  serviceName: 'admin-fe'
});
const provider = new WebTracerProvider({
  resource: new Resource({
    [SRA.SERVICE_NAME]: 'admin-fe',
    [SRA.DEPLOYMENT_ENVIRONMENT]: 'dev',
  }),
  spanProcessors: [
    new BatchSpanProcessor(exporter),
    new BatchSpanProcessor(zipkinExporter),
  ],
});

provider.register({
  propagator: new W3CTraceContextPropagator(),
});
