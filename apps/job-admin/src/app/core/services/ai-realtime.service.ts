import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import {firstValueFrom} from 'rxjs';
import { NotificationService } from './notification.service';
import { AccountService } from './account.service';
import { environment } from '../../../environments/environment';
import {propagation, ROOT_CONTEXT, Span, SpanKind, SpanStatusCode, trace} from '@opentelemetry/api';
import { AiNotificationDto } from '../types/Dtos/AiNotificationDto';
import {Router} from '@angular/router';
import {CompanyService} from './company.service';
import {CompanySelectionStore} from '../../shared/companies/company-selection/company-selection.store';
import {JobsStore} from '../../features/jobs/jobs.store';

@Injectable({ providedIn: 'root' })
export class AiRealtimeService {
  private hub?: signalR.HubConnection;
  private notify = inject(NotificationService);
  private account = inject(AccountService);
  private companyService = inject(CompanyService);
  private companySelectionStore = inject(CompanySelectionStore);
  private jobsStore = inject(JobsStore);
  private router = inject(Router);
  private tracer = trace.getTracer('admin-fe');
  private starting = false;

  connected = signal(false);
  latestNotification = signal<AiNotificationDto | null>(null);

  async start() {
    if (this.hub || this.starting) return;
    this.starting = true;

    const hubUrl = `${environment.aiServiceUrl}hubs/notifications`;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: async () =>
          await firstValueFrom(
            this.account.auth.getAccessTokenSilently()
          ),
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('ai.notification', (msg: AiNotificationDto) => {
      const carrier: Record<string, string> = {};
      if (msg.traceParent) carrier['traceparent'] = msg.traceParent;
      if (msg.traceState)  carrier['tracestate']  = msg.traceState;
      const parentCtx = propagation.extract(ROOT_CONTEXT, carrier);
      this.tracer.startActiveSpan(
        'signalr.ai.notification.received',
        {
          kind: SpanKind.CONSUMER,
          attributes: {
            'messaging.system': 'signalr',
            'messaging.operation': 'process',
            'messaging.destination.name': 'ai.notification',
            'ai.notification.type': msg.type,
            'ai.notification.entityType': msg.entityType,
            'ai.notification.entityId': msg.entityId,
          },
        },
        parentCtx,
        async (span) => {
          try {
            if (msg.correlationId) {
              span.setAttribute('correlation.id', msg.correlationId);
            }
            this.latestNotification.set(msg);
            switch (msg.type) {
              case 'draft.generated':
                await this.handleDraftGenerated(msg, span);
                break;
              default:
                this.notify.info('AI Notification', msg.type);
            }
            span.setStatus({ code: SpanStatusCode.OK });
          } catch (err: any) {
            span.recordException(err);
            span.setStatus({ code: SpanStatusCode.ERROR, message: err?.message });
            throw err;
          } finally {
            span.end();
          }
        }
      );
    });

    this.hub.onreconnecting(() => {
      this.connected.set(false);
      this.notify.info('AI Reconnecting…', 'Trying to restore AI realtime connection.');
    });

    this.hub.onreconnected(() => {
      this.connected.set(true);
      this.notify.info('AI Connected', 'AI realtime connection restored.');
    });

    this.hub.onclose((err) => {
      this.connected.set(false);
      if (err) {
        console.error('AI SignalR closed:', err);
        this.notify.warn('AI Realtime offline', 'AI notifications hub disconnected.');
      }
    });

    try {
      await this.hub.start();
      this.connected.set(true);
    } catch (err) {
      this.connected.set(false);
      this.notify.warn('AI Realtime offline', 'Could not connect to AI notifications hub.');
      console.error(err);
    } finally {
      this.starting = false;
    }
  }

  private async handleDraftGenerated(msg: AiNotificationDto, span: Span) {
    const companyName = msg.metadata?.['companyName']?.toString();
    const companyId = msg.metadata?.['companyId']?.toString();
    const draftId = msg.entityId;

    if (companyName) {
      this.notify.success('Draft Generated', `Draft Generated for ${companyName}`);
    } else {
      this.notify.success('Draft Generated', `Draft Generated`);
    }

    if (companyId && companyId !== '') {
      // Hydrate companies + drafts, select the company, then navigate to the draft
      const res = await firstValueFrom(this.companyService.listCompanies());
      const company = res.data?.find(c => c.uId === companyId);
      if (!company) return;

      this.companySelectionStore.selectedCompany.set(company);

      await firstValueFrom(this.jobsStore.loadDrafts(companyId));

      if (this.router.url !== `/jobs/new/${draftId}`) {
        span.setAttribute('ui.action', 'navigate');
        span.setAttribute('ui.route', `/jobs/new/${draftId}`);
        await this.router.navigate(['/jobs/new', draftId]);
      }
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
