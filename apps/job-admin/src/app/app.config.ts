import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, APP_INITIALIZER } from '@angular/core';
import { TracingErrorHandler } from './core/error-handler/tracing-error-handler';
import {provideRouter, TitleStrategy, withComponentInputBinding, withViewTransitions} from '@angular/router';
import {EnvTitleStrategy} from './core/services/env-title.strategy';
import Aura from '@primeng/themes/aura';
import { routes } from './app.routes';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
} from '@angular/common/http';
import {provideAnimationsAsync} from '@angular/platform-browser/animations/async';
import {providePrimeNG} from 'primeng/config';
import {tracingInterceptor} from './core/interceptores/tracing.interceptor';
import {MessageService} from 'primeng/api';
import {idempotencyInterceptor} from './core/interceptores/idempotency/idempotency.interceptor';
import {DialogService} from 'primeng/dynamicdialog';
import {authInterceptor, provideAuth, LogLevel, OidcSecurityService} from 'angular-auth-oidc-client';
import {environment} from '../environments/environment';
import {firstValueFrom} from 'rxjs';
import {initFaro} from '../faro';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    { provide: ErrorHandler, useClass: TracingErrorHandler },
    provideHttpClient(withFetch(), withInterceptors([authInterceptor(), tracingInterceptor, idempotencyInterceptor])),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    { provide: TitleStrategy, useClass: EnvTitleStrategy },
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
    provideAuth({
      config: {
        authority: environment.oidc.authority,
        redirectUrl: window.location.origin,
        postLogoutRedirectUri: window.location.origin,
        clientId: environment.oidc.clientId,

        scope: 'openid profile email offline_access',
        responseType: 'code',

        silentRenew: true,
        useRefreshToken: true,

        logLevel: environment.production ? LogLevel.None : LogLevel.Debug,

        ignoreNonceAfterRefresh: true,
        disableRefreshIdTokenAuthTimeValidation: true,


        secureRoutes: [
          environment.gatewayUrl,
          environment.monolithUrl,
          environment.microserviceUrl,
          environment.aiServiceUrl,
        ],
      },
    }),
    DialogService,
    {
      provide: APP_INITIALIZER,
      useFactory: (oidc: OidcSecurityService) => () => firstValueFrom(oidc.checkAuth()),
      deps: [OidcSecurityService],
      multi: true,
    },
    {
      // Initialize Grafana Faro RUM. Returns Promise<void> and never blocks
      // app startup on Faro/geo failures (initFaro swallows errors internally).
      provide: APP_INITIALIZER,
      useFactory: () => () => initFaro(),
      multi: true,
    },
  ]
};
