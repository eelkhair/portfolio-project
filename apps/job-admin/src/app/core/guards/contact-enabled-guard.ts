import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { FeatureFlagsService } from '../services/feature-flags.service';

/**
 * Route guard for the /contact feature. Allows access only when the
 * `ContactForm` feature flag is on. When the flag is off, redirects to /dashboard.
 * Pairs with `FeatureFlagsService.contactForm` signal.
 */
export const contactEnabledGuard: CanActivateFn = () => {
  const featureFlags = inject(FeatureFlagsService);
  const router = inject(Router);
  if (featureFlags.contactForm()) {
    return true;
  }
  return router.createUrlTree(['/dashboard']);
};
