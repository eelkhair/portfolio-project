// Grafana Faro RUM bootstrap. Replaces the previous direct OTel WebTracerProvider
// setup; Faro installs the global tracer provider so the existing
// `tracingInterceptor` and `TracingErrorHandler` continue to work, with their
// spans now flowing through the Faro pipeline → Alloy → OTel collector → Jaeger.
//
// Geo (country/city/region/lat/lon) is fetched once cross-origin from landing's
// `/api/geo` and stamped onto every span via a custom span processor that
// wraps Faro's default chain.
//
// No-ops when `environment.faroUrl` is empty (local dev without RUM).

import {
  initializeFaro,
  getWebInstrumentations,
  faro,
  LogLevel,
} from '@grafana/faro-web-sdk';
import {
  TracingInstrumentation,
  FaroTraceExporter,
  FaroMetaAttributesSpanProcessor,
} from '@grafana/faro-web-tracing';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-web';
import type { Context } from '@opentelemetry/api';
import type { Span, ReadableSpan, SpanProcessor } from '@opentelemetry/sdk-trace-base';
import { environment } from './environments/environment';
import { scrubPiiDeep } from './pii-hasher';

type GeoData = {
  country: string;
  city: string;
  region: string;
  lat: number | null;
  lon: number | null;
};

const EMPTY_GEO: GeoData = { country: 'XX', city: '', region: '', lat: null, lon: null };

let initialized = false;

/**
 * Wraps Faro's default span pipeline so we can stamp every span with `geo.*`
 * attributes (resolved at app startup from landing's /api/geo). They flow into
 * the OTel collector's spanmetrics connector via matching dimensions and end
 * up as Prometheus labels for the RUM dashboard.
 *
 * We rebuild Faro's default chain (FaroMetaAttributesSpanProcessor wrapping
 * BatchSpanProcessor + FaroTraceExporter) because the `spanProcessor` option
 * REPLACES the default. Inner chain is built lazily on first span so that
 * `faro.api` / `faro.metas` are populated.
 */
function buildGeoSpanProcessor(geo: GeoData): SpanProcessor {
  let inner: SpanProcessor | null = null;
  const ensure = (): SpanProcessor => {
    if (inner) return inner;
    inner = new FaroMetaAttributesSpanProcessor(
      new BatchSpanProcessor(new FaroTraceExporter({ api: faro.api }), {
        scheduledDelayMillis: 1000,
        maxExportBatchSize: 30,
      }),
      faro.metas,
    );
    return inner;
  };

  return {
    onStart(span: Span, parentContext: Context): void {
      span.setAttribute('geo.country_code', geo.country);
      if (geo.city) span.setAttribute('geo.city', geo.city);
      if (geo.region) span.setAttribute('geo.region', geo.region);
      if (geo.lat !== null) span.setAttribute('geo.lat', geo.lat);
      if (geo.lon !== null) span.setAttribute('geo.lon', geo.lon);
      ensure().onStart(span, parentContext);
    },
    onEnd(span: ReadableSpan): void {
      ensure().onEnd(span);
    },
    shutdown(): Promise<void> {
      return ensure().shutdown();
    },
    forceFlush(): Promise<void> {
      return ensure().forceFlush();
    },
  };
}

async function fetchGeo(url: string): Promise<GeoData> {
  if (!url) return EMPTY_GEO;
  try {
    const r = await fetch(url, { credentials: 'omit' });
    if (!r.ok) return EMPTY_GEO;
    const data = (await r.json()) as Partial<GeoData>;
    return {
      country: (data.country ?? 'XX').toUpperCase(),
      city: data.city ?? '',
      region: data.region ?? '',
      lat: typeof data.lat === 'number' ? data.lat : null,
      lon: typeof data.lon === 'number' ? data.lon : null,
    };
  } catch {
    return EMPTY_GEO;
  }
}

/**
 * APP_INITIALIZER target. Resolves once Faro is set up (or once it's been
 * decided to skip). Always resolves — we never block app startup on Faro.
 */
export async function initFaro(): Promise<void> {
  if (initialized) return;
  const url = environment.faroUrl;
  if (!url) return;

  const geo = await fetchGeo(environment.geoApiUrl);

  initializeFaro({
    url,
    app: {
      name: 'admin-fe',
      environment: environment.envName.toLowerCase(),
    },
    // Hash email/phone/first/last/full/user-name values in every outgoing
    // transport item (logs, events, measurements, exceptions) before the
    // Alloy → OTel → ES pipeline sees them. Matches backend PiiHasher format
    // (pii_ + 12 hex) so correlation by hash still works across FE/BE.
    beforeSend: (item) => {
      try {
        return { ...item, payload: scrubPiiDeep(item.payload) };
      } catch {
        return item;
      }
    },
    instrumentations: [
      ...getWebInstrumentations({ captureConsole: true }),
      new TracingInstrumentation({
        // Disable Faro's default fetch + XHR auto-instrumentations. Our
        // Angular `tracingInterceptor` already creates HTTP spans for every
        // HttpClient request; keeping auto-instrumentation on produced a
        // duplicate `component=fetch` span per request that cluttered the
        // RUM dashboard's Recent traces panel.
        instrumentations: [],
        spanProcessor: buildGeoSpanProcessor(geo),
      }),
    ],
  });

  initialized = true;
  faro.api.pushLog(['Faro initialized'], { level: LogLevel.INFO });
}
