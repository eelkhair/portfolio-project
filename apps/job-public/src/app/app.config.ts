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
            clientId: '32VHi7fNpZeUvHcYhM85fvVBRq9U38xV',
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
