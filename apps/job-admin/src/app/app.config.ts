import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import {provideRouter, withComponentInputBinding, withViewTransitions} from '@angular/router';
import Aura from '@primeng/themes/aura';
import { routes } from './app.routes';
import {

  provideHttpClient,
  withFetch,
  withInterceptors,
} from '@angular/common/http';
import {provideAuth0} from '@auth0/auth0-angular';
import {provideAnimationsAsync} from '@angular/platform-browser/animations/async';
import {providePrimeNG} from 'primeng/config';
import {authInterceptor} from './core/interceptores/auth.interceptor';
import {tracingInterceptor} from './core/interceptores/tracing.interceptor';
import {MessageService} from 'primeng/api';
import {idempotencyInterceptor} from './core/interceptores/idempotency/idempotency.interceptor';
import {DialogService} from 'primeng/dynamicdialog';


export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, tracingInterceptor, idempotencyInterceptor])),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    provideAnimationsAsync(),
    providePrimeNG({
      ripple: true,
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: '.dark',
          cssLayer: {
            name: 'primeng',
            order: 'theme, base, primeng'
          }
        }
      }
    }),
    MessageService,
    provideAuth0({
      domain: 'elkhair-dev.us.auth0.com',
      clientId: 'YXnqj0gOfZJD8Ypje7mdZqdoenCHNzWA',
      authorizationParams: {
        audience: 'https://job-board.eelkhair.net',      // << crucial
        redirect_uri: window.location.origin,
        scope: 'openid profile email offline_access'
      },
      cacheLocation: 'localstorage',
      useRefreshTokens: true
    }),
    DialogService
  ]
};
