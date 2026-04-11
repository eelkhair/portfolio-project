import { Component, DestroyRef, inject, Injector, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { filter, map, switchMap, take } from 'rxjs';
import { Header } from './layout/header/header';
import { Footer } from './layout/footer/footer';
import { Chat } from './shared/chat/chat';
import { DebugSidebar } from './shared/debug-sidebar/debug-sidebar';
import { ResumeRealtimeService } from './core/services/resume-realtime.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer, Chat, DebugSidebar],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly injector = inject(Injector);
  private readonly resumeRt = inject(ResumeRealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const oidc = this.injector.get(OidcSecurityService, null);
    if (!oidc) return;

    // Only start SignalR after the user has a valid access token
    const sub = oidc.isAuthenticated$.pipe(
      map(r => r.isAuthenticated),
      filter(Boolean),
      switchMap(() => oidc.getAccessToken()),
      filter(token => !!token),
      take(1),
    ).subscribe(() => {
      this.resumeRt.start();
    });

    this.destroyRef.onDestroy(() => {
      void this.resumeRt.stop();
      sub.unsubscribe();
    });
  }
}
