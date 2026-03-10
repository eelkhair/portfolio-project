import {Injectable} from '@angular/core';
import {OidcSecurityService} from 'angular-auth-oidc-client';

@Injectable({ providedIn: 'root' })
export class AuthService {

  constructor(private oidc: OidcSecurityService) {}

  login(): void {
    this.oidc.authorize();
  }

  logout(): void {
    this.oidc.logoff();
  }

  get isAuthenticated$() {
    return this.oidc.isAuthenticated$;
  }

  get userData$() {
    return this.oidc.userData$;
  }
}
