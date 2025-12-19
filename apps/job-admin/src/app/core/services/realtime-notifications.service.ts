import { Injectable, signal, inject, effect } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from './notification.service';
import {AccountService} from './account.service';
import {environment} from '../../../environments/environment';
import {propagation, ROOT_CONTEXT, SpanKind, SpanStatusCode, trace} from '@opentelemetry/api';
import {FeatureFlagsDto} from '../types/Dtos/FeatureFlagsDto';
import {FeatureFlagsService} from './feature-flags.service';

export interface CompanyActivatedMsg {
  companyUId: string;
  companyName: string;
  message: string;
  traceParent?: string;
  traceState?: string;
}

@Injectable({ providedIn: 'root' })
export class RealtimeNotificationsService {
  private hub?: signalR.HubConnection;
  private notify = inject(NotificationService);
  private account = inject(AccountService);
  private tracer = trace.getTracer('admin-fe')
  private featureFlagService = inject(FeatureFlagsService)
  connected = signal(false);
  companyActivated = signal<CompanyActivatedMsg | null>(null);
  private starting = false;
  private currentTopology: 'monolith' | 'micro' | null = null;

  constructor() {
    effect(()=>{
      if (!this.featureFlagService.featureFlags()) return;

      const topology = this.featureFlagService.isMonolith()
        ? 'monolith'
        : 'micro';

      if (this.currentTopology === topology) return;

      this.currentTopology = topology;

      void this.stop();
      void this.start();
    })
  }
  async start() {
    if (this.hub || this.starting) return; // idempotent
    this.starting = true;
    const baseUrl = this.featureFlagService.isMonolith()
      ? environment.monolithUrl
      : environment.microserviceUrl;
    const hubUrl = `${baseUrl}hubs/notifications`;
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

    this.hub.on('featureFlagsUpdated', (msg: {flags: FeatureFlagsDto})=>{
      this.featureFlagService.setFlags(msg.flags);
    });
    this.hub.on('CompanyActivated', (msg: CompanyActivatedMsg) => {
      const carrier: Record<string, string> = {};
      if (msg.traceParent) carrier['traceparent'] = msg.traceParent;
      if (msg.traceState)  carrier['tracestate']  = msg.traceState;
      const parentCtx = propagation.extract(ROOT_CONTEXT, carrier);
      this.tracer.startActiveSpan(
        'signalr.message.received',
        {
          kind: SpanKind.CONSUMER,
          attributes: {
            'messaging.system': 'signalr',
            'messaging.operation': 'process',
            'messaging.destination.name': 'CompanyActivated',
            'company.id': msg.companyUId,
            'company.name': msg.companyName,
          },
        },
        parentCtx,
        (span) => {
        try {
          this.companyActivated.set(msg);
          this.notify.success('Company activated', `“${msg.companyName}” is now active.`);

          span.setStatus({ code: SpanStatusCode.OK });
        } catch (err: any) {
          span.recordException(err);
          span.setStatus({ code: SpanStatusCode.ERROR, message: err?.message });
          throw err;
        } finally {
          span.end();
        }
      });
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
