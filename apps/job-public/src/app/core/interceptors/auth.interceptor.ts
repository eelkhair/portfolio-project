import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, first, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Endpoints that don't require authentication */
const PUBLIC_PATHS = ['/api/public'];

/**
 * SSR-safe HTTP interceptor that attaches Keycloak Bearer tokens
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

  const oidc = inject(OidcSecurityService, { optional: true });
  if (!oidc) {
    return next(req);
  }

  return oidc.getAccessToken().pipe(
    first(),
    switchMap((token) => {
      if (!token) {
        return next(req);
      }
      const authReq = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      });
      return next(authReq);
    }),
    catchError((err) => throwError(() => err)),
  );
};
