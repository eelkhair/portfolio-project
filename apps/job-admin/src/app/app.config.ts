import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import {provideRouter, withComponentInputBinding, withViewTransitions} from '@angular/router';
import Aura from '@primeng/themes/aura';
import { routes } from './app.routes';
import {provideHttpClient, withFetch, withInterceptors} from '@angular/common/http';
import {provideAuth0} from '@auth0/auth0-angular';
import {provideAnimationsAsync} from '@angular/platform-browser/animations/async';
import {providePrimeNG} from 'primeng/config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideHttpClient(withFetch(), withInterceptors([])),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    provideAnimationsAsync(),


    providePrimeNG({
      ripple: true,
      theme: {
        preset: Aura,                       // or Aura
        options: {
          darkModeSelector: '.app-dark',    // optional
          cssLayer: {                       // ✅ correct place (no double 'options')
            name: 'primeng',
            order: 'theme, base, primeng'
          }
        }
      }
    }),
    provideAuth0({
      domain: 'elkhair-dev.us.auth0.com',           // e.g., dev-abc123.us.auth0.com
      clientId: 'YXnqj0gOfZJD8Ypje7mdZqdoenCHNzWA',      // from Auth0 dashboard
      authorizationParams: { redirect_uri: window.location.origin },
      cacheLocation: 'localstorage',          // helps with Safari/3rd-party cookies
      useRefreshTokens: true
    }),
  ]
};
