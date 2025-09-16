import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from './notification.service';
import {AccountService} from './account.service';
import {environment} from '../../../environments/environment';
import {toSignal} from '@angular/core/rxjs-interop';
 // wherever your auth lives

export interface CompanyActivatedMsg {
  companyUId: string;
  companyName: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class RealtimeNotificationsService {
  private hub?: signalR.HubConnection;
  private notify = inject(NotificationService);
  private account = inject(AccountService);

  // reactive bits (optional)
  connected = signal(false);
  companyActivated = signal<CompanyActivatedMsg | null>(null);
  private token = toSignal(this.account.auth.getAccessTokenSilently());

  private starting = false;

  async start(hubUrl = environment.apiUrl + 'hubs/notifications') {
    if (this.hub || this.starting) return; // idempotent
    this.starting = true;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: async () =>
          await firstValueFrom(
            this.account.auth.getAccessTokenSilently()
          ),
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning) // change to Information for debugging
      .build();

    this.hub.on('CompanyActivated', (msg: CompanyActivatedMsg) => {
      this.companyActivated.set(msg);
      this.notify.success('Company activated', `“${msg.companyName}” is now active.`);
    });

    this.hub.onreconnecting(() => {
      this.connected.set(false);
      // optional: one-time toast on first reconnect attempt
      this.notify.info('Reconnecting…', 'Trying to restore realtime connection.');
    });

    this.hub.onreconnected(() => {
      this.connected.set(true);
      this.notify.info('Connected', 'Realtime connection restored.');
    });

    this.hub.onclose((err) => {
      this.connected.set(false);
      if (err) {
        console.error('SignalR closed:', err);
        this.notify.warn('Realtime offline', 'Notifications hub disconnected.');
      }
      // leave reconnection to withAutomaticReconnect
    });

    try {
      await this.hub.start();
      this.connected.set(true);
    } catch (err) {
      this.connected.set(false);
      this.notify.warn('Realtime offline', 'Could not connect to notifications hub.');
      console.error(err);
    } finally {
      this.starting = false;
    }
  }

  async stop() {
    if (!this.hub) return;
    const h = this.hub;
    this.hub = undefined;
    this.connected.set(false);
    await h.stop();
  }
}
