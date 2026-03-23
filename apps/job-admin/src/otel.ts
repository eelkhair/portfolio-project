// src/otel.ts
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor} from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes as SRA } from '@opentelemetry/semantic-conventions';
import {environment} from './environments/environment';
import { W3CTraceContextPropagator } from '@opentelemetry/core';

const exporter = new OTLPTraceExporter({ url: environment.otel });
const provider = new WebTracerProvider({
  resource: new Resource({
    [SRA.SERVICE_NAME]: 'admin-fe',
    [SRA.DEPLOYMENT_ENVIRONMENT]: 'dev',
  }),
  spanProcessors: [
    new BatchSpanProcessor(exporter),
  ],
});

provider.register({
  propagator: new W3CTraceContextPropagator(),
});
