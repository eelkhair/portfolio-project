import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, SEMRESATTRS_DEPLOYMENT_ENVIRONMENT } from '@opentelemetry/semantic-conventions';
import { W3CTraceContextPropagator } from '@opentelemetry/core';
import { environment } from './environments/environment';

if (typeof window !== 'undefined') {
  const exporter = new OTLPTraceExporter({ url: environment.otel });

  const provider = new WebTracerProvider({
    resource: resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'public-fe',
      [SEMRESATTRS_DEPLOYMENT_ENVIRONMENT]: 'dev',
    }),
    spanProcessors: [
      new BatchSpanProcessor(exporter),
    ],
  });

  provider.register({
    propagator: new W3CTraceContextPropagator(),
  });
}
