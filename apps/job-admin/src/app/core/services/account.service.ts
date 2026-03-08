import {computed, inject, Injectable} from '@angular/core';
import {OidcSecurityService, LoginResponse} from 'angular-auth-oidc-client';
import {toSignal} from '@angular/core/rxjs-interop';
import {Observable} from 'rxjs';
import {map} from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private oidc = inject(OidcSecurityService);

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

  checkAuth(): Observable<LoginResponse> {
    return this.oidc.checkAuth();
  }

  getAccessToken(): Observable<string> {
    return this.oidc.getAccessToken();
  }

  logout() {
    this.oidc.logoff().subscribe();
  }
}
