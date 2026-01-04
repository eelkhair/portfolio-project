import { HttpInterceptorFn } from '@angular/common/http';
import { from } from 'rxjs';
import { mergeMap } from 'rxjs/operators';
import { IDEMPOTENCY_DISABLE, IDEMPOTENCY_FORCE_KEY } from './idempotency.context';
import { stableStringify, sha256Hex } from './idempotency.util';
import { IdempotencyCache } from './idempotency.cache';

const cache = new IdempotencyCache();

export const idempotencyInterceptor: HttpInterceptorFn = (req, next) => {
  if (!['POST','PUT','PATCH','DELETE'].includes(req.method)) return next(req);
  if (req.context.get(IDEMPOTENCY_DISABLE)) return next(req);

  const forced = req.context.get(IDEMPOTENCY_FORCE_KEY);
  if (forced && !req.headers.has('Idempotency-Key')) {
    return next(req.clone({ setHeaders: { 'Idempotency-Key': forced } }));
  }
  if (req.headers.has('Idempotency-Key')) return next(req);

  const contentType = req.detectContentTypeHeader() || req.headers.get('Content-Type') || '';
  const isJson =
    contentType.includes('application/json') ||
    (req.body && !(req.body instanceof FormData) && typeof req.body === 'object');

  if (!isJson) {
    const key = crypto.randomUUID();
    return next(req.clone({ setHeaders: { 'Idempotency-Key': key } }));
  }

  const bodyStr = typeof req.body === 'string' ? req.body : stableStringify(req.body ?? {});
  return from(sha256Hex(`${req.method}|${req.urlWithParams}|${bodyStr}`)).pipe(
    mergeMap(fp => {
      const key = cache.getOrCreate(fp);
      const cloned = req.clone({ setHeaders: { 'Idempotency-Key': key } });
      return next(cloned);
    })
  );
};
