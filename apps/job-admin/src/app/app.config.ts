import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, APP_INITIALIZER } from '@angular/core';
import { TracingErrorHandler } from './core/error-handler/tracing-error-handler';
import {provideRouter, withComponentInputBinding, withViewTransitions} from '@angular/router';
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


export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    { provide: ErrorHandler, useClass: TracingErrorHandler },
    provideHttpClient(withFetch(), withInterceptors([authInterceptor(), tracingInterceptor, idempotencyInterceptor])),
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
    {
      provide: APP_INITIALIZER,
      useFactory: (oidc: OidcSecurityService) => () => firstValueFrom(oidc.checkAuth()),
      deps: [OidcSecurityService],
      multi: true,
    },
    provideAuth({
      config: {
        authority: environment.oidc.authority,
        redirectUrl: environment.oidc.redirectUrl,
        postLogoutRedirectUri: environment.oidc.redirectUrl,
        clientId: environment.oidc.clientId,
        scope: 'openid profile email offline_access',
        responseType: 'code',
        silentRenew: true,
        useRefreshToken: true,
        logLevel: environment.production ? LogLevel.None : LogLevel.Debug,
        secureRoutes: [
          environment.gatewayUrl,
          environment.monolithUrl,
          environment.microserviceUrl,
          environment.aiServiceUrl,
        ],
      },
    }),
    DialogService
  ]
};
