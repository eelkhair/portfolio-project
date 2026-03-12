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

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    { provide: ErrorHandler, useClass: TracingErrorHandler },
    provideRouter(routes),
    { provide: TitleStrategy, useClass: EnvTitleStrategy },
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, tracingInterceptor])),
    provideClientHydration(withEventReplay()),
    ...(typeof window !== 'undefined'
      ? [
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
          {
            provide: APP_INITIALIZER,
            useFactory: (oidc: OidcSecurityService) => () => firstValueFrom(oidc.checkAuth()),
            deps: [OidcSecurityService],
            multi: true,
          },
        ]
      : []),
  ],
};
