// src/app/tracing.interceptor.ts
import {
  HttpInterceptorFn,
  HttpResponse,
  HttpErrorResponse,
} from '@angular/common/http';
import { trace, SpanKind, SpanStatusCode, Span } from '@opentelemetry/api';
import { tap, finalize } from 'rxjs/operators';

const EXCLUDE: RegExp[] = [
  /\/v1\/traces$/i,
  /\/api\/v2\/spans$/i,
  /^assets\//i,
  /\.woff2?$|\.png$|\.jpe?g$|\.svg$|\.css$|\.js$/i,
  /(^|\/)healthz(\?|$)/i,
];

export const tracingInterceptor: HttpInterceptorFn = (req, next) => {
  const urlStr = req.url.toString();

  if (EXCLUDE.some(re => re.test(urlStr))) {
    return next(req);
  }

  const tracer = trace.getTracer('admin-fe');
  const span: Span = tracer.startSpan(`HTTP ${req.method} ${path(urlStr)}`, {
    kind: SpanKind.CLIENT,
    attributes: {
      'http.method': req.method,
      'http.url': scrub(urlStr),
    },
  });

  const headers: Record<string, string> = {};
  if (!req.headers.has('traceparent')) headers['traceparent'] = toTraceparent(span);
  const tracedReq = Object.keys(headers).length ? req.clone({ setHeaders: headers }) : req;

  const t0 = performance.now();

  return next(tracedReq).pipe(
    tap({
      next: evt => {
        if (evt instanceof HttpResponse) {
          span.setAttribute('http.status_code', evt.status);
          if (evt.status >= 400) {
            span.setStatus({ code: SpanStatusCode.ERROR });
          }
        }
      },
      error: (err: HttpErrorResponse) => {
        span.setAttribute('http.status_code', err.status || 0);
        span.recordException(err);
        span.setStatus({ code: SpanStatusCode.ERROR, message: err.message });
      },
    }),
    finalize(() => {
      span.setAttribute('http.duration_ms', Math.round(performance.now() - t0));
      span.end();
    })
  );
};

/* helpers */
function scrub(url: string){ try{ const u=new URL(url, location.origin); u.search=''; return u.toString(); } catch { return url; } }
function path(url: string){ try{ return new URL(url, location.origin).pathname; } catch { return url; } }
function toTraceparent(span: Span){ const c = span.spanContext(); const s = (c.traceFlags & 1) === 1 ? '01' : '00'; return `00-${c.traceId}-${c.spanId}-${s}`; }
