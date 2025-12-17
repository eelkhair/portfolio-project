import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Header} from './layout/header/header';
import {Footer} from './layout/footer/footer';
import {RouterOutlet} from '@angular/router';
import {Nav} from './layout/nav/nav';
import {AccountService} from './core/services/account.service';
import {ToastModule} from 'primeng/toast';
import {RealtimeNotificationsService} from './core/services/realtime-notifications.service';
import {environment} from '../environments/environment';
import {JsonPipe} from '@angular/common';
import {FeatureFlagsService} from './core/services/feature-flags.service';

@Component({
  selector: 'app-root',
  imports: [
    Header,
    Footer,
    RouterOutlet,
    ToastModule,
    Nav,
    JsonPipe
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  accountService = inject(AccountService);
  featureFlagService = inject(FeatureFlagsService);
  destroyRef = inject(DestroyRef);
  protected rt = inject(RealtimeNotificationsService);
  ngOnInit() {
    const sub =this.accountService.auth.getAccessTokenSilently().subscribe(()=>
      this.rt.start(environment.apiUrl + 'hubs/notifications' )
    );
    this.destroyRef.onDestroy(()=>{
      void this.rt.stop();
      sub.unsubscribe();
    })

  }

}
