import { computed, inject, Injectable, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly oidc = inject(OidcSecurityService, { optional: true });

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

  getAccessToken(): Observable<string> {
    return this.oidc?.getAccessToken() ?? of('');
  }

  login(): void {
    this.oidc?.authorize();
  }

  logout(): void {
    sessionStorage.removeItem('job-public-tour-shown');
    this.oidc?.logoff().subscribe();
  }
}
