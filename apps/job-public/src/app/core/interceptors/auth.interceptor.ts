import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';
import { switchMap, first, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Endpoints that don't require authentication */
const PUBLIC_PATHS = ['/api/public'];

/**
 * SSR-safe HTTP interceptor that attaches Auth0 Bearer tokens
 * to requests targeting the backend API.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return next(req);
  }

  // Only attach tokens to our APIs
  if (!req.url.startsWith(environment.apiUrl) && !req.url.startsWith(environment.aiUrl)) {
    return next(req);
  }

  // Allow public endpoints without auth
  if (PUBLIC_PATHS.some((p) => req.url.includes(p))) {
    return next(req);
  }

  const auth = inject(AuthService, { optional: true });
  if (!auth) {
    return next(req);
  }

  return auth.getAccessTokenSilently().pipe(
    first(),
    switchMap((token) => {
      const authReq = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      });
      return next(authReq);
    }),
    catchError((err) => throwError(() => err)),
  );
};
