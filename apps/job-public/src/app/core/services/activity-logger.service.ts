import { Injectable } from '@angular/core';
import { MonoTypeOperatorFunction, Observable } from 'rxjs';
import { faro, LogLevel } from '@grafana/faro-web-sdk';
import { context, trace, SpanKind, SpanStatusCode } from '@opentelemetry/api';

const APP_TAG = 'public';
const TRACER_NAME = 'public-fe';
const INVALID_TRACE_ID = '00000000000000000000000000000000';

type SpanCtx = { traceId: string; spanId: string };

/**
 * Lightweight activity logger. Uses `faro.api.pushLog` so context fields land
 * in Seq + ES as structured properties (instead of being string-joined into
 * the message as `[object Object]` like `captureConsole` does), and attaches
 * a `spanContext` so the resulting log lines correlate by `TraceId` in the
 * "Find by Trace Id" Grafana dashboard.
 *
 * The `trace<T>()` operator creates its own activity span and makes it active
 * during subscribe — so the HTTP span the `tracingInterceptor` creates
 * downstream becomes a child of it, sharing the same traceId. The activity
 * log lines emitted on success / error use that same traceId.
 *
 * For standalone `info / warn / error` calls (auth lifecycle), we probe
 * `trace.getActiveSpan()` — if the call is happening inside an already-active
 * context, the log inherits its trace id automatically.
 *
 * Falls back to `console.*` when Faro hasn't initialized (local dev where
 * `faroUrl` is empty, SSR, or pre-init in the bootstrap window).
 *
 * No PII (titles, descriptions, message bodies) -- ids, counts, and outcomes
 * only. Unhandled exceptions are already covered by `TracingErrorHandler`;
 * call `error()` from explicit catch blocks only to avoid double-logging.
 */
@Injectable({ providedIn: 'root' })
export class ActivityLogger {
  info(event: string, ctx?: Record<string, unknown>, spanCtx?: SpanCtx): void {
    this.push(LogLevel.INFO, event, ctx, undefined, spanCtx);
  }

  warn(event: string, ctx?: Record<string, unknown>, spanCtx?: SpanCtx): void {
    this.push(LogLevel.WARN, event, ctx, undefined, spanCtx);
  }

  error(event: string, error: unknown, ctx?: Record<string, unknown>, spanCtx?: SpanCtx): void {
    const errCtx: Record<string, unknown> = { ...(ctx ?? {}) };
    if (error instanceof Error) {
      errCtx['errorType'] = error.constructor.name;
      errCtx['errorMessage'] = error.message;
      if (error.stack) errCtx['errorStack'] = error.stack;
    } else if (error !== undefined) {
      errCtx['errorType'] = typeof error;
      errCtx['errorMessage'] = String(error);
    }
    this.push(LogLevel.ERROR, event, errCtx, error, spanCtx);
  }

  /**
   * RxJS operator that brackets an Observable with success/failure logs and a
   * duration in ms. Creates an internal "activity" span so:
   *   1. The HTTP span the tracingInterceptor creates downstream gets parented
   *      to it (sharing one trace id end-to-end).
   *   2. The activity log lines explicitly carry that trace id via spanContext.
   * Errors are logged then re-thrown so downstream subscribers still see them.
   */
  trace<T>(
    event: string,
    ctxBuilder?: (value: T) => Record<string, unknown>,
  ): MonoTypeOperatorFunction<T> {
    return (source: Observable<T>) =>
      new Observable<T>((subscriber) => {
        const tracer = trace.getTracer(TRACER_NAME);
        const span = tracer.startSpan(`activity ${event}`, { kind: SpanKind.INTERNAL });
        const spanCtx = toSpanCtx(span.spanContext());
        const started = now();

        let spanEnded = false;
        const endSpan = (status?: { ok: boolean; message?: string }) => {
          if (spanEnded) return;
          spanEnded = true;
          if (status && !status.ok) {
            span.setStatus({ code: SpanStatusCode.ERROR, message: status.message });
          }
          span.end();
        };

        const sub = context.with(trace.setSpan(context.active(), span), () =>
          source.subscribe({
            next: (value) => {
              const elapsedMs = Math.round(now() - started);
              const extra = ctxBuilder ? ctxBuilder(value) : undefined;
              this.info(`${event} ok`, { elapsedMs, ...(extra ?? {}) }, spanCtx);
              subscriber.next(value);
            },
            error: (err) => {
              const elapsedMs = Math.round(now() - started);
              this.error(`${event} fail`, err, { elapsedMs }, spanCtx);
              if (err instanceof Error) span.recordException(err);
              endSpan({ ok: false, message: err instanceof Error ? err.message : String(err) });
              subscriber.error(err);
            },
            complete: () => {
              endSpan();
              subscriber.complete();
            },
          }),
        );

        return () => {
          sub.unsubscribe();
          endSpan();
        };
      });
  }

  private push(
    level: LogLevel,
    event: string,
    ctx?: Record<string, unknown>,
    rawError?: unknown,
    explicitSpanCtx?: SpanCtx,
  ): void {
    const message = `[${APP_TAG}] ${event}`;
    const logContext = ctx ? stringifyContext(ctx) : undefined;
    const spanContext = explicitSpanCtx ?? activeSpanCtx();

    try {
      if (faro?.api?.pushLog) {
        faro.api.pushLog([message], {
          level,
          ...(logContext ? { context: logContext } : {}),
          ...(spanContext ? { spanContext } : {}),
        });
        return;
      }
    } catch {
      // fall through
    }

    const fn =
      level === LogLevel.ERROR ? console.error
        : level === LogLevel.WARN ? console.warn
          : console.info;
    if (rawError !== undefined && level === LogLevel.ERROR) {
      ctx ? fn(message, rawError, ctx) : fn(message, rawError);
    } else {
      ctx ? fn(message, ctx) : fn(message);
    }
  }
}

function now(): number {
  return typeof performance !== 'undefined' ? performance.now() : Date.now();
}

function activeSpanCtx(): SpanCtx | undefined {
  try {
    const span = trace.getActiveSpan();
    if (!span) return undefined;
    return toSpanCtx(span.spanContext());
  } catch {
    return undefined;
  }
}

function toSpanCtx(ctx: { traceId?: string; spanId?: string } | undefined): SpanCtx | undefined {
  if (!ctx?.traceId || !ctx.spanId) return undefined;
  if (ctx.traceId === INVALID_TRACE_ID) return undefined;
  return { traceId: ctx.traceId, spanId: ctx.spanId };
}

/**
 * Faro's LogContext requires Record<string, string>. Coerce numbers / booleans
 * via String(); JSON-stringify objects; drop null/undefined entries.
 */
function stringifyContext(ctx: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(ctx)) {
    if (v === null || v === undefined) continue;
    if (typeof v === 'string') out[k] = v;
    else if (typeof v === 'number' || typeof v === 'boolean' || typeof v === 'bigint') out[k] = String(v);
    else {
      try { out[k] = JSON.stringify(v); }
      catch { out[k] = String(v); }
    }
  }
  return out;
}
