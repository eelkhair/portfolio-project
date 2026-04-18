"use client";

import { ReactNode, useEffect } from "react";
import { initializeFaro, getWebInstrumentations, faro, LogLevel } from "@grafana/faro-web-sdk";
import {
  TracingInstrumentation,
  FaroTraceExporter,
  FaroMetaAttributesSpanProcessor,
} from "@grafana/faro-web-tracing";
import { BatchSpanProcessor } from "@opentelemetry/sdk-trace-web";
import type { Context } from "@opentelemetry/api";
import type { Span, ReadableSpan, SpanProcessor } from "@opentelemetry/sdk-trace-base";
import type { GeoData } from "../lib/geo";

let initialized = false;

/**
 * Wraps Faro's default span pipeline so we can stamp every span with `geo.*`
 * attributes (resolved server-side in layout.tsx). They flow into the OTel
 * collector's spanmetrics connector via matching dimensions and end up as
 * Prometheus labels for the RUM dashboard.
 *
 * Rebuilds Faro's default chain (FaroMetaAttributesSpanProcessor wrapping
 * BatchSpanProcessor + FaroTraceExporter) because the `spanProcessor` option
 * REPLACES the default. Inner chain is built lazily on first span so that
 * `faro.api` / `faro.metas` are populated (initializeFaro hasn't returned at
 * processor-construction time).
 */
function buildSpanProcessor(geo: GeoData): SpanProcessor {
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
      span.setAttribute("geo.country_code", geo.country);
      if (geo.city) span.setAttribute("geo.city", geo.city);
      if (geo.region) span.setAttribute("geo.region", geo.region);
      if (geo.lat !== null) span.setAttribute("geo.lat", geo.lat);
      if (geo.lon !== null) span.setAttribute("geo.lon", geo.lon);
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

export function FaroProvider({
  children,
  env,
  geo,
}: {
  children: ReactNode;
  env: string;
  geo: GeoData;
}) {
  useEffect(() => {
    if (initialized) return;

    const url = process.env.NEXT_PUBLIC_FARO_URL;
    if (!url) return;

    const appName = process.env.NEXT_PUBLIC_FARO_APP_NAME ?? "landing";

    initializeFaro({
      url,
      app: {
        name: appName,
        environment: env,
      },
      instrumentations: [
        ...getWebInstrumentations({ captureConsole: true }),
        new TracingInstrumentation({
          spanProcessor: buildSpanProcessor(geo),
        }),
      ],
    });

    initialized = true;
    faro.api.pushLog(["Faro initialized"], { level: LogLevel.INFO });
  }, [env, geo]);

  return <>{children}</>;
}
