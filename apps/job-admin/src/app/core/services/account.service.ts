import {computed, inject, Injectable} from '@angular/core';
import {OidcSecurityService, LoginResponse} from 'angular-auth-oidc-client';
import {toSignal} from '@angular/core/rxjs-interop';
import {Observable} from 'rxjs';
import {map, distinctUntilChanged, pairwise, startWith, filter, take} from 'rxjs/operators';
import {ActivityLogger} from './activity-logger.service';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private oidc = inject(OidcSecurityService);
  private logger = inject(ActivityLogger);

  isAuthenticated = toSignal(
    this.oidc.isAuthenticated$.pipe(map(result => result.isAuthenticated)),
    { initialValue: false }
  );

  /** Stays true once the user has authenticated at least once — prevents layout flicker during token refresh */
  hasInitialized = computed(() => {
    if (this.isAuthenticated()) this._everAuthenticated = true;
    return this._everAuthenticated;
  });
  private _everAuthenticated = false;

  user = toSignal(
    this.oidc.userData$.pipe(map(result => result.userData as Record<string, any> | undefined)),
  );

  groups = computed(() => {
    const userData = this.user();
    return (userData?.['groups'] as string[]) ?? [];
  });

  isAdmin = computed(() => this.groups().some(g => g.replace(/^\//, '') === 'Admins'));

  /** Extract company UIDs from group paths like /Companies/{uid}/... or Companies/{uid}/... */
  companyUIds = computed(() =>
    this.groups()
      .map(g => g.replace(/^\//, ''))
      .filter(g => g.startsWith('Companies/'))
      .map(g => g.split('/')[1])
      .filter((uid): uid is string => !!uid)
      .filter((uid, i, arr) => arr.indexOf(uid) === i)
  );

  constructor() {
    // Auth lifecycle logs: login = false->true transition, logout = true->false.
    // pairwise() needs a seed value so the first emission also fires.
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

  checkAuth(): Observable<LoginResponse> {
    return this.oidc.checkAuth();
  }

  getAccessToken(): Observable<string> {
    return this.oidc.getAccessToken();
  }

  logout() {
    this.logger.info('auth logout requested');
    sessionStorage.removeItem('job-admin-getting-started-shown');
    sessionStorage.removeItem('job-admin-tour-shown');
    this.oidc.logoff().subscribe();
  }
}
