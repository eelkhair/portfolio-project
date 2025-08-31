import {inject, Injectable} from '@angular/core';
import {AuthService} from '@auth0/auth0-angular';
import {toSignal} from '@angular/core/rxjs-interop';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  auth = inject(AuthService);
  isAuthenticated = toSignal(this.auth.isAuthenticated$, { initialValue: false });
  user = toSignal(this.auth.user$, { initialValue: null });

  login() {
    this.auth.loginWithRedirect();
  }

  logout() {
    this.auth.logout({
      logoutParams: { returnTo: window.location.origin }
    });
  }

}
