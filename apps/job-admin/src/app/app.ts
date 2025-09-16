import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Header} from './layout/header/header';
import {Footer} from './layout/footer/footer';
import {RouterOutlet} from '@angular/router';
import {Nav} from './layout/nav/nav';
import {AccountService} from './core/services/account.service';
import {ToastModule} from 'primeng/toast';
import {RealtimeNotificationsService} from './core/services/realtime-notifications.service';
import {environment} from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [
    Header,
    Footer,
    RouterOutlet,
    ToastModule,
    Nav,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  accountService = inject(AccountService);
  destroyRef = inject(DestroyRef);
  private rt = inject(RealtimeNotificationsService);
  ngOnInit() {
    const sub =this.accountService.auth.getAccessTokenSilently().subscribe(token=>
      this.rt.start(environment.apiUrl + 'hubs/notifications' )
    );
    this.destroyRef.onDestroy(()=>{
      this.rt.stop();
      sub.unsubscribe();
    })

  }

}
