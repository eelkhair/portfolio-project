import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import {provideHttpClient} from '@angular/common/http';
import {provideAuth0} from '@auth0/auth0-angular';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideHttpClient(),
    provideRouter(routes),
    provideAuth0({
      domain: 'elkhair-dev.us.auth0.com',           // e.g., dev-abc123.us.auth0.com
      clientId: 'YXnqj0gOfZJD8Ypje7mdZqdoenCHNzWA',      // from Auth0 dashboard
      authorizationParams: {
        redirect_uri: window.location.origin,
      }
    }),
  ]
};
