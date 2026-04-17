/**
 * Next.js 15 instrumentation hook — registers OpenTelemetry on server startup.
 *
 * `@vercel/otel` wires up auto-instrumentation for:
 *  - Incoming HTTP route handlers (/, /contact, /api/contact, /portfolio, etc.)
 *  - Outbound fetch() calls (Resend API, Turnstile siteverify)
 *
 * Exporter config is read from standard OTEL_* env vars (see compose files):
 *  - OTEL_EXPORTER_OTLP_ENDPOINT — base URL of the OTLP HTTP collector
 *  - OTEL_SERVICE_NAME — identifies traces in Jaeger/Grafana
 *  - OTEL_RESOURCE_ATTRIBUTES — e.g. deployment.environment=dev|prod
 *
 * The `register()` export name is a Next.js contract; do not rename.
 */
import { registerOTel } from '@vercel/otel';

export function register() {
  registerOTel({
    serviceName: process.env.OTEL_SERVICE_NAME ?? 'landing',
  });
}
