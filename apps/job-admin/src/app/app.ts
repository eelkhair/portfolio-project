import {Component, DestroyRef, effect, inject, OnInit} from '@angular/core';
import {Header} from './layout/header/header';
import {Footer} from './layout/footer/footer';
import {RouterOutlet} from '@angular/router';
import {Nav} from './layout/nav/nav';
import {AccountService} from './core/services/account.service';
import {ToastModule} from 'primeng/toast';
import {RealtimeNotificationsService} from './core/services/realtime-notifications.service';
import {AiRealtimeService} from './core/services/ai-realtime.service';
import {AiChat} from './shared/ai-chat/ai-chat';
import {DebugSidebar} from './shared/debug-sidebar/debug-sidebar';
import {OidcSecurityService} from 'angular-auth-oidc-client';
import {GettingStarted} from './layout/getting-started/getting-started';

@Component({
  selector: 'app-root',
  imports: [
    Header,
    Footer,
    RouterOutlet,
    ToastModule,
    Nav,
    AiChat,
    DebugSidebar,
    GettingStarted
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  accountService = inject(AccountService);
  destroyRef = inject(DestroyRef);
  protected rt = inject(RealtimeNotificationsService);
  protected aiRt = inject(AiRealtimeService);

  private signalRStarted = false;
  constructor() {
    // Auth callback is handled by APP_INITIALIZER in app.config.ts
    // Start SignalR hubs when authentication state becomes true
    effect(() => {
      if (this.accountService.isAuthenticated() && !this.signalRStarted) {
        this.signalRStarted = true;
        this.rt.start();
        this.aiRt.start();
      }
    });

    this.destroyRef.onDestroy(() => {
      void this.rt.stop();
      void this.aiRt.stop();
    });
  }
}
