import { HttpInterceptorFn, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { trace, SpanKind, SpanStatusCode, Span } from '@opentelemetry/api';
import { finalize, tap } from 'rxjs/operators';

export const tracingInterceptor: HttpInterceptorFn = (req, next) => {
  const tracer = trace.getTracer('job-admin-fe');

  const span: Span = tracer.startSpan(`HTTP ${req.method} ${path(req.url)}`, {
    kind: SpanKind.CLIENT,
    attributes: { 'http.method': req.method, 'http.url': scrub(req.url) },
  });

  const tp = traceparent(span);
  const tracedReq = req.clone({ setHeaders: { traceparent: tp } });

  const t0 = performance.now();
  return next(tracedReq).pipe(
    tap({
      next: (evt) => {
        if (evt instanceof HttpResponse) {
          span.setAttribute('http.status_code', evt.status);
          if (evt.status >= 400) span.setStatus({ code: SpanStatusCode.ERROR });
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

function scrub(url: string){ try{const u=new URL(url,location.origin);u.search='';return u.toString();}catch{ return url; } }
function path(url: string){ try{ return new URL(url,location.origin).pathname; }catch{ return url; } }
function traceparent(span: Span){ const c=span.spanContext(); const s=(c.traceFlags&1)===1?'01':'00'; return `00-${c.traceId}-${c.spanId}-${s}`; }
