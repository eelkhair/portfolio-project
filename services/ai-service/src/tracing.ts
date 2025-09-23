// tracing.ts
import { trace } from '@opentelemetry/api';
export const tracer = trace.getTracer('ai-service'); // name is arbitrary
