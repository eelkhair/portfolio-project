import { inject, Injectable, Injector, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';
import { firstValueFrom } from 'rxjs';
import { propagation, ROOT_CONTEXT, SpanKind, SpanStatusCode, trace } from '@opentelemetry/api';
import { environment } from '../../../environments/environment';
import { ApplicationStore } from '../stores/application.store';
import { ProfileStore } from '../stores/profile.store';
import { ResumeParsedMsg, ResumeParseFailedMsg } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class ResumeRealtimeService {
  private hub?: import('@microsoft/signalr').HubConnection;
  private readonly platformId = inject(PLATFORM_ID);
  private readonly injector = inject(Injector);
  private readonly store = inject(ApplicationStore);
  private readonly profileStore = inject(ProfileStore);
  private readonly tracer = trace.getTracer('public-fe');
  private starting = false;
  private visibilityHandler?: () => void;

  readonly connected = signal(false);

  async start(): Promise<void> {
    if (!isPlatformBrowser(this.platformId) || this.hub || this.starting) return;
    this.starting = true;

    const signalR = await import('@microsoft/signalr');

    const auth = this.injector.get(AuthService, null);

    const hubUrl = `${environment.monolithUrl}hubs/notifications`;
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: async () => {
          if (!auth) return '';
          return await firstValueFrom(auth.getAccessTokenSilently());
        },
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('ResumeParsed', (msg: ResumeParsedMsg) => {
      const parentCtx = this.extractTraceContext(msg);
      this.tracer.startActiveSpan(
        'signalr.message.received',
        {
          kind: SpanKind.CONSUMER,
          attributes: {
            'messaging.system': 'signalr',
            'messaging.operation': 'process',
            'messaging.destination.name': 'ResumeParsed',
            'resume.id': msg.resumeId,
          },
        },
        parentCtx,
        (span) => {
          try {
            this.store.onResumeParsed(msg.resumeId, msg.currentPage, msg.traceParent);
            this.profileStore.onResumeParsed(msg.resumeId, msg.traceParent);
            span.setStatus({ code: SpanStatusCode.OK });
          } catch (err: any) {
            span.recordException(err);
            span.setStatus({ code: SpanStatusCode.ERROR, message: err?.message });
          } finally {
            span.end();
          }
        },
      );
    });

    this.hub.on('ResumeParseFailed', (msg: ResumeParseFailedMsg) => {
      const parentCtx = this.extractTraceContext(msg);
      this.tracer.startActiveSpan(
        'signalr.message.received',
        {
          kind: SpanKind.CONSUMER,
          attributes: {
            'messaging.system': 'signalr',
            'messaging.operation': 'process',
            'messaging.destination.name': 'ResumeParseFailed',
            'resume.id': msg.resumeId,
            'resume.parse.status': msg.status,
            'resume.parse.attempt': msg.attempt,
          },
        },
        parentCtx,
        (span) => {
          try {
            this.store.onResumeParseFailed(msg.status === 'retrying');
            this.profileStore.onResumeParseFailed(msg.status === 'retrying');
            span.setStatus({ code: SpanStatusCode.OK });
          } catch (err: any) {
            span.recordException(err);
            span.setStatus({ code: SpanStatusCode.ERROR, message: err?.message });
          } finally {
            span.end();
          }
        },
      );
    });

    this.hub.onreconnecting(() => this.connected.set(false));
    this.hub.onreconnected(() => {
      this.connected.set(true);
      this.recoverPendingParses();
    });
    this.hub.onclose(() => this.connected.set(false));

    this.visibilityHandler = () => {
      if (document.visibilityState === 'visible') {
        this.recoverPendingParses();
      }
    };
    document.addEventListener('visibilitychange', this.visibilityHandler);

    try {
      await this.hub.start();
      this.connected.set(true);
    } catch (err) {
      this.connected.set(false);
      console.error('Resume realtime connection failed:', err);
    } finally {
      this.starting = false;
    }
  }

  async stop(): Promise<void> {
    if (this.visibilityHandler) {
      document.removeEventListener('visibilitychange', this.visibilityHandler);
      this.visibilityHandler = undefined;
    }
    if (!this.hub) return;
    const h = this.hub;
    this.hub = undefined;
    this.connected.set(false);
    await h.stop();
  }

  /** Poll API for completed parses that were missed while tab was backgrounded */
  private recoverPendingParses(): void {
    this.store.recoverIfParsing();
    this.profileStore.recoverIfParsing();
  }

  private extractTraceContext(msg: { traceParent?: string; traceState?: string }) {
    const carrier: Record<string, string> = {};
    if (msg.traceParent) carrier['traceparent'] = msg.traceParent;
    if (msg.traceState) carrier['tracestate'] = msg.traceState;
    return propagation.extract(ROOT_CONTEXT, carrier);
  }
}
