"use client";

import { ReactNode, useEffect } from "react";
import { initializeFaro, getWebInstrumentations, faro, LogLevel } from "@grafana/faro-web-sdk";
import {
  TracingInstrumentation,
  FaroTraceExporter,
  FaroMetaAttributesSpanProcessor,
} from "@grafana/faro-web-tracing";
import { BatchSpanProcessor } from "@opentelemetry/sdk-trace-web";
import { trace, type Context } from "@opentelemetry/api";
import type { Span, ReadableSpan, SpanProcessor } from "@opentelemetry/sdk-trace-base";
import type { GeoData } from "../lib/geo";
import { scrubPiiDeep } from "../lib/pii-hasher";

const INVALID_TRACE_ID = "00000000000000000000000000000000";

let initialized = false;

/** Emit a structured log via faro.api.pushLog so context fields land as
 *  top-level properties in ES instead of being string-joined into the
 *  message by `captureConsole`. Attaches the active span context so the
 *  log correlates by TraceId in the "Find by Trace Id" dashboard. */
function pushStructured(message: string, ctx: Record<string, unknown>): void {
  const context: Record<string, string> = {};
  for (const [k, v] of Object.entries(ctx)) {
    if (v === null || v === undefined) continue;
    if (typeof v === "string") context[k] = v;
    else if (typeof v === "number" || typeof v === "boolean") context[k] = String(v);
    else {
      try { context[k] = JSON.stringify(v); }
      catch { context[k] = String(v); }
    }
  }
  let spanContext: { traceId: string; spanId: string } | undefined;
  try {
    const span = trace.getActiveSpan();
    const sc = span?.spanContext();
    if (sc?.traceId && sc.spanId && sc.traceId !== INVALID_TRACE_ID) {
      spanContext = { traceId: sc.traceId, spanId: sc.spanId };
    }
  } catch {
    /* no active context */
  }
  try {
    faro.api.pushLog([message], {
      level: LogLevel.INFO,
      context,
      ...(spanContext ? { spanContext } : {}),
    });
  } catch {
    console.info(message, ctx);
  }
}

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
      // Hash email/phone/first/last/full/user-name values in every outgoing
      // transport item (logs, events, measurements, exceptions) before the
      // Alloy → OTel → ES pipeline sees them. Matches backend PiiHasher
      // format (pii_ + 12 hex) so correlation by hash still works across
      // FE/BE.
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
          spanProcessor: buildSpanProcessor(geo),
        }),
      ],
    });

    initialized = true;
    faro.api.pushLog(["Faro initialized"], { level: LogLevel.INFO });

    // Emit a page-view log with geo + page context. Flows through
    // Faro → Alloy → OTel Collector → ES (for FE services),
    // where the Find by Trace Id dashboard can correlate it with the span.
    // Use pushLog directly with `context` so values land as structured
    // properties rather than `[object Object]` from console arg-joining.
    pushStructured("[landing] page view", {
      url: typeof window !== "undefined" ? window.location.href : "",
      path: typeof window !== "undefined" ? window.location.pathname : "",
      referrer: typeof document !== "undefined" ? document.referrer : "",
      country: geo.country,
      city: geo.city || undefined,
      region: geo.region || undefined,
    });

    // Log route changes for SPA navigation. The Next.js App Router fires
    // popstate on back/forward and updates history.pushState on link clicks.
    if (typeof window !== "undefined") {
      const origPush = history.pushState.bind(history);
      history.pushState = function (...args) {
        const result = origPush(...args);
        pushStructured("[landing] route change", {
          from: document.referrer || undefined,
          to: window.location.pathname,
        });
        return result;
      };
      window.addEventListener("popstate", () => {
        pushStructured("[landing] route back/forward", { to: window.location.pathname });
      });
    }
  }, [env, geo]);

  return <>{children}</>;
}
