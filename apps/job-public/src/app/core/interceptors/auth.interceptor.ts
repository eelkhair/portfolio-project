import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';
import { switchMap, first, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * SSR-safe HTTP interceptor that attaches Auth0 Bearer tokens
 * to requests targeting the backend API.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return next(req);
  }

  // Only attach tokens to our API
  if (!req.url.startsWith(environment.apiUrl)) {
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
    catchError(() => next(req)),
  );
};
