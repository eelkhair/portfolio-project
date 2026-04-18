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

let initialized = false;

/**
 * Mutable holder for geo so the span processor can pick up city/region after
 * the async `/api/geo` lookup resolves. The country is seeded synchronously
 * from the SSR-rendered Cloudflare header, so first spans always have it.
 */
const geo = {
  country: "XX",
  city: "",
  region: "",
};

/**
 * Wraps Faro's default span pipeline. Stamps every span with `geo.*`
 * attributes that flow into the OTel collector's spanmetrics connector
 * (configured with matching dimensions) so the Grafana RUM dashboard
 * can chart visitors by country and city.
 *
 * We rebuild Faro's default chain (FaroMetaAttributesSpanProcessor wrapping
 * BatchSpanProcessor + FaroTraceExporter) because the `spanProcessor` option
 * REPLACES the default. The inner chain is built lazily on first span so
 * that `faro.api` / `faro.metas` are populated (initializeFaro hasn't
 * returned yet at processor-construction time).
 */
function buildSpanProcessor(): SpanProcessor {
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
  country,
}: {
  children: ReactNode;
  env: string;
  country: string;
}) {
  useEffect(() => {
    if (initialized) return;

    const url = process.env.NEXT_PUBLIC_FARO_URL;
    if (!url) return;

    geo.country = country;

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
          spanProcessor: buildSpanProcessor(),
        }),
      ],
    });

    initialized = true;
    faro.api.pushLog(["Faro initialized"], { level: LogLevel.INFO });

    // Async city lookup; updates the holder so spans created after this
    // resolves get richer geo. First page-load spans only have country.
    (async () => {
      try {
        const r = await fetch("/api/geo", { cache: "no-store" });
        if (!r.ok) return;
        const data = (await r.json()) as { country?: string; city?: string; region?: string };
        if (data.country) geo.country = data.country.toUpperCase();
        if (data.city) geo.city = data.city;
        if (data.region) geo.region = data.region;
      } catch {
        // Best-effort. Country from SSR is already on the holder.
      }
    })();
  }, [env, country]);

  return <>{children}</>;
}
