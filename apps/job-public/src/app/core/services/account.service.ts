import { computed, inject, Injectable, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, Observable, of } from 'rxjs';
import { distinctUntilChanged, filter, pairwise, startWith, take } from 'rxjs/operators';
import { ActivityLogger } from './activity-logger.service';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly oidc = inject(OidcSecurityService, { optional: true });
  private readonly logger = inject(ActivityLogger);

  private _everAuthenticated = false;

  readonly isAuthenticated = this.oidc
    ? toSignal(this.oidc.isAuthenticated$.pipe(map(r => r.isAuthenticated)), { initialValue: false })
    : signal(false);

  readonly hasInitialized = computed(() => {
    if (this.isAuthenticated()) this._everAuthenticated = true;
    return this._everAuthenticated;
  });

  readonly user = this.oidc
    ? toSignal(this.oidc.userData$.pipe(map(r => r.userData as Record<string, any> | undefined)))
    : signal(undefined);

  readonly displayName = computed(() => {
    const u = this.user();
    return u?.['given_name'] ?? u?.['name'] ?? u?.['preferred_username'] ?? u?.['email'] ?? 'User';
  });

  readonly groups = computed(() => {
    const userData = this.user();
    return (userData?.['groups'] as string[]) ?? [];
  });

  constructor() {
    if (!this.oidc) return;

    // Auth lifecycle logs: login = false->true transition, logout = true->false.
    this.oidc.isAuthenticated$
      .pipe(
        map(r => r.isAuthenticated),
        distinctUntilChanged(),
        startWith(false),
        pairwise(),
      )
      .subscribe(([prev, curr]) => {
        if (!prev && curr) this.logger.info('auth login');
        else if (prev && !curr) this.logger.info('auth logout');
      });

    // First non-empty user payload — emit claim summary (no values, just keys/counts)
    this.oidc.userData$
      .pipe(
        map(r => r.userData as Record<string, unknown> | undefined | null),
        filter((u): u is Record<string, unknown> => !!u && Object.keys(u).length > 0),
        take(1),
      )
      .subscribe(u => {
        const groups = (u['groups'] as string[] | undefined) ?? [];
        this.logger.info('auth user data', {
          claimKeys: Object.keys(u).length,
          groupsCount: groups.length,
          hasEmail: 'email' in u,
        });
      });
  }

  getAccessToken(): Observable<string> {
    return this.oidc?.getAccessToken() ?? of('');
  }

  login(): void {
    this.logger.info('auth login requested');
    this.oidc?.authorize();
  }

  logout(): void {
    this.logger.info('auth logout requested');
    sessionStorage.removeItem('job-public-tour-shown');
    this.oidc?.logoff().subscribe();
  }
}
