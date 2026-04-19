import { Component, inject, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Router } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-auth-callback',
  template: `<p>Signing you in...</p>`,
})
export class AuthCallbackComponent implements OnInit {
  // SSR-safe: OidcSecurityService is only provided when `typeof window !== 'undefined'`
  // in app.config.ts. On the server, keep this optional and short-circuit ngOnInit —
  // the real auth check runs on the client after hydration. A non-optional inject
  // here throws NG0201 during SSR bootstrap.
  private oidc = inject(OidcSecurityService, { optional: true });
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId) || !this.oidc) {
      return;
    }
    this.oidc.checkAuth().subscribe({
      next: ({ isAuthenticated }) => {
        if (isAuthenticated) {
          this.router.navigateByUrl('/');
        } else {
          this.router.navigateByUrl('/login');
        }
      },
      error: (error) => {
        console.error('OIDC callback failed', error);
        this.router.navigateByUrl('/login');
      },
    });
  }
}
