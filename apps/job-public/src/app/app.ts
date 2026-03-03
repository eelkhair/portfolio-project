import { Component, DestroyRef, inject, Injector, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { Header } from './layout/header/header';
import { Footer } from './layout/footer/footer';
import { ResumeRealtimeService } from './core/services/resume-realtime.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer],
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

    const auth = this.injector.get(AuthService, null);
    if (!auth) return;

    const sub = auth.getAccessTokenSilently().subscribe(() => {
      this.resumeRt.start();
    });

    this.destroyRef.onDestroy(() => {
      void this.resumeRt.stop();
      sub.unsubscribe();
    });
  }
}
