import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Header} from './layout/header/header';
import {Footer} from './layout/footer/footer';
import {RouterOutlet} from '@angular/router';
import {Nav} from './layout/nav/nav';
import {AccountService} from './core/services/account.service';
import {ToastModule} from 'primeng/toast';
import {RealtimeNotificationsService} from './core/services/realtime-notifications.service';
import {AiRealtimeService} from './core/services/ai-realtime.service';
import {AiChat} from './shared/ai-chat/ai-chat';

@Component({
  selector: 'app-root',
  imports: [
    Header,
    Footer,
    RouterOutlet,
    ToastModule,
    Nav,
    AiChat
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  accountService = inject(AccountService);
  destroyRef = inject(DestroyRef);
  protected rt = inject(RealtimeNotificationsService);
  protected aiRt = inject(AiRealtimeService);
  ngOnInit() {
    const sub =this.accountService.auth.getAccessTokenSilently().subscribe(()=> {
      this.rt.start();
      this.aiRt.start();
    });
    this.destroyRef.onDestroy(()=>{
      void this.rt.stop();
      void this.aiRt.stop();
      sub.unsubscribe();
    })

  }

}
