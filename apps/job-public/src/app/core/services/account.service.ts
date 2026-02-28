import { computed, inject, Injectable, signal } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { toSignal } from '@angular/core/rxjs-interop';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly auth = inject(AuthService, { optional: true });

  readonly isAuthenticated = this.auth
    ? toSignal(this.auth.isAuthenticated$, { initialValue: false })
    : signal(false);

  readonly user = this.auth ? toSignal(this.auth.user$) : signal(undefined);

  readonly displayName = computed(() => {
    const u = this.user();
    return u?.given_name ?? u?.name ?? u?.nickname ?? u?.email ?? 'User';
  });

  login(): void {
    this.auth?.loginWithRedirect();
  }

  logout(): void {
    this.auth?.logout({
      logoutParams: { returnTo: window.location.origin },
    });
  }
}
