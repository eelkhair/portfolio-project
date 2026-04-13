import { Component, inject, OnInit } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Router } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-auth-callback',
  templateUrl: './auth-callback.html'
})
export class AuthCallbackComponent implements OnInit {
  private oidc = inject(OidcSecurityService);
  private router = inject(Router);

  ngOnInit(): void {
    this.oidc.checkAuth().subscribe({
      next: ({ isAuthenticated }) => {
        alert(isAuthenticated);
        if (isAuthenticated) {
          this.router.navigateByUrl('/');
        } else {
          this.router.navigateByUrl('/login');
        }
      },
      error: (error) => {
        console.error('OIDC callback failed', error);
        this.router.navigateByUrl('/login');
      }
    });
  }
}
