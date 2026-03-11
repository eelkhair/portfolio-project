import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CanActivateFn } from '@angular/router';
import { AutoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';
import { of } from 'rxjs';

/**
 * SSR-safe auth guard.
 * On the server, OIDC is not provided — allow the route to render so SSR
 * can return markup. The client-side hydration will enforce the real auth check.
 */
export const authGuard: CanActivateFn = (...args) => {
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return of(true);
  }

  const oidcGuard = inject(AutoLoginPartialRoutesGuard);
  return oidcGuard.canActivate(...(args as [any, any]));
};
