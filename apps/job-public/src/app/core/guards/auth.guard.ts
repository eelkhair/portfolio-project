import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CanActivateFn } from '@angular/router';
import { AuthGuard as Auth0Guard } from '@auth0/auth0-angular';
import { of } from 'rxjs';

/**
 * SSR-safe auth guard.
 * On the server, Auth0 is not provided — allow the route to render so SSR
 * can return markup. The client-side hydration will enforce the real auth check.
 */
export const authGuard: CanActivateFn = (...args) => {
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return of(true);
  }

  const auth0Guard = inject(Auth0Guard);
  return auth0Guard.canActivate(...(args as [any, any]));
};
