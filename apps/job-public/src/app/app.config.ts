import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideAuth0 } from '@auth0/auth0-angular';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    provideClientHydration(withEventReplay()),
    ...(typeof window !== 'undefined'
      ? [
          provideAuth0({
            domain: 'elkhair-dev.us.auth0.com',
            clientId: 'YXnqj0gOfZJD8Ypje7mdZqdoenCHNzWA',
            authorizationParams: {
              audience: 'https://job-board.eelkhair.net',
              redirect_uri: window.location.origin,
              scope: 'openid profile email',
            },
            cacheLocation: 'localstorage',
            useRefreshTokens: true,
          }),
        ]
      : []),
  ],
};
