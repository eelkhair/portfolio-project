import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { FeatureFlagsService } from '../services/feature-flags.service';

/**
 * Blocks the `/contact` route when the `ContactForm` feature flag is off.
 * Flag is delivered via SignalR on authenticated connection, so this guard
 * should be chained after `authGuard`.
 */
export const contactEnabledGuard: CanActivateFn = () => {
  const featureFlags = inject(FeatureFlagsService);
  const router = inject(Router);
  if (featureFlags.contactForm()) {
    return true;
  }
  return router.createUrlTree(['/']);
};
