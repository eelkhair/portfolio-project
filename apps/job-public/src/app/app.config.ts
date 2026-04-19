import {
  APP_INITIALIZER,
  ApplicationConfig,
  ErrorHandler,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { EnvTitleStrategy } from './core/services/env-title.strategy';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAuth, LogLevel, OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { tracingInterceptor } from './core/interceptors/tracing.interceptor';
import { TracingErrorHandler } from './core/error-handler/tracing-error-handler';
import { environment } from '../environments/environment';
import { initFaro } from '../faro';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    { provide: ErrorHandler, useClass: TracingErrorHandler },
    provideRouter(routes),
    { provide: TitleStrategy, useClass: EnvTitleStrategy },
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, tracingInterceptor])),
    provideClientHydration(withEventReplay()),
    // `provideAuth()` MUST be registered unconditionally. Several OIDC services
    // (ConfigurationService, PeriodicallyTokenCheckService, etc.) are
    // `providedIn: 'root'`, so Angular's hydration path may construct them on
    // the SSR server. Their constructors inject OIDC-internal tokens (loader,
    // authWellKnownService, ...) that ONLY `provideAuth()` registers — gating
    // this with `typeof window !== 'undefined'` causes NG0201 at SSR hydration.
    // The providers themselves are safe on the server; window/document access
    // happens only when services are USED (checkAuth, storage), which we still
    // guard via platform checks in guards, interceptors, and components.
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
        secureRoutes: [environment.apiUrl, environment.aiUrl, environment.monolithUrl],
      },
    }),
    // APP_INITIALIZERs remain browser-only — checkAuth() and Faro both touch
    // window/storage/fetch and must not run on the SSR server.
    ...(typeof window !== 'undefined'
      ? [
          {
            provide: APP_INITIALIZER,
            useFactory: (oidc: OidcSecurityService) => () => firstValueFrom(oidc.checkAuth()),
            deps: [OidcSecurityService],
            multi: true,
          },
          {
            // Initialize Grafana Faro RUM (browser-only). Returns Promise<void>
            // and never blocks app startup on Faro/geo failures.
            provide: APP_INITIALIZER,
            useFactory: () => () => initFaro(),
            multi: true,
          },
        ]
      : []),
  ],
};
